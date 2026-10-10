using System.Reflection;
using NetArchTest.Rules;

namespace Casevanta.Architecture.Tests;

/// <summary>
/// Dependency rules from ADR-0005 (module boundaries). Each rule is a test, so a violating
/// reference fails CI instead of relying on review. Rules about business modules apply to every
/// Casevanta.Modules.* assembly the tests can load; until the first module exists they have
/// nothing to check.
/// </summary>
public sealed class DependencyRules
{
    private static readonly Assembly Api = Assembly.Load("Casevanta.Api");
    private static readonly Assembly ServiceDefaults = Assembly.Load("Casevanta.ServiceDefaults");

    [Fact]
    public void AR01_The_api_does_not_depend_on_the_orchestration_layer() =>
        AssertNoDependency(Api, "Aspire.Hosting");

    [Fact]
    public void AR02_Service_defaults_do_not_depend_on_the_api() =>
        AssertNoDependency(ServiceDefaults, "Casevanta.Api");

    [Fact]
    public void AR03_Business_modules_do_not_depend_on_the_api_host()
    {
        foreach (var module in ModuleAssemblies())
        {
            AssertNoDependency(module, "Casevanta.Api");
        }
    }

    [Fact]
    public void AR04_Business_modules_use_each_other_only_through_their_contracts()
    {
        var modules = ModuleAssemblies();
        foreach (var module in modules)
        {
            foreach (var other in modules.Where(candidate => candidate != module))
            {
                var internalNamespaces = other.GetTypes()
                    .Select(type => type.Namespace)
                    .OfType<string>()
                    .Where(ns => !ns.Split('.').Contains("Contracts"))
                    .Distinct()
                    .ToArray();

                if (internalNamespaces.Length > 0)
                {
                    AssertNoDependency(module, internalNamespaces);
                }
            }
        }
    }

    private static Assembly[] ModuleAssemblies() =>
        Directory.EnumerateFiles(AppContext.BaseDirectory, "Casevanta.Modules.*.dll")
            .Select(path => Assembly.LoadFrom(path))
            .ToArray();

    private static void AssertNoDependency(Assembly assembly, params string[] forbidden)
    {
        var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();
        var offenders = result.FailingTypeNames ?? [];
        Assert.True(result.IsSuccessful,
            $"{assembly.GetName().Name} must not depend on {string.Join(", ", forbidden)}; offending types: {string.Join(", ", offenders)}");
    }
}
