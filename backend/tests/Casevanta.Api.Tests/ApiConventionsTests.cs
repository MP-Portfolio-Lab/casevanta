using System.Net;
using System.Text.Json;
using Casevanta.Api.Tests.Infrastructure;

namespace Casevanta.Api.Tests;

/// <summary>API error contract and exposure rules (ADR-0007; threat model T9).</summary>
[Collection(BffCollection.Name)]
public sealed class ApiConventionsTests(BffFactory factory)
{
    [Fact]
    public async Task A14_Unknown_api_routes_return_problem_details()
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.GetAsync("/api/does-not-exist");

        await AssertProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A26_Unhandled_api_errors_return_problem_details_without_internal_details()
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.GetAsync("/api/test-only/unhandled");

        var body = await AssertProblem(response, HttpStatusCode.InternalServerError);
        Assert.DoesNotContain(FaultInjectionController.SensitiveDetail, body);
        Assert.DoesNotContain("InvalidOperationException", body);
        Assert.DoesNotContain(" at ", body); // no stack trace
    }

    [Fact]
    public async Task A27_Authentication_and_authorization_failures_return_problem_details()
    {
        using var anonymous = factory.CreateBrowser();
        await AssertProblem(await anonymous.GetAsync("/api/verification/ping"), HttpStatusCode.Unauthorized);

        using var employee = factory.CreateBrowser();
        factory.SignIn(employee, name: "Ada Employee", subject: "user-employee", "employee");
        await AssertProblem(await employee.GetAsync("/api/verification/ping"), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A28_The_openapi_document_is_not_exposed_outside_development()
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<string> AssertProblem(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.BodyAsync();
        using var problem = JsonDocument.Parse(body);
        Assert.Equal((int)status, problem.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(problem.RootElement.GetProperty("title").GetString()));
        return body;
    }
}
