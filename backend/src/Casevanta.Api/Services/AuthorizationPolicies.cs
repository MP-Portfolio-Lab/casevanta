namespace Casevanta.Api.Services;

/// <summary>
/// Named authorization policies. Endpoints refer to policies, never to role names (ADR-0006):
/// the identity provider supplies coarse roles, and Casevanta decides what they allow.
/// </summary>
public static class AuthorizationPolicies
{
    public const string Verifier = nameof(Verifier);
}

/// <summary>Realm roles issued by the workforce identity provider (realm casevanta-workforce).</summary>
public static class Roles
{
    public const string Employee = "employee";
    public const string Verifier = "verifier";
    public const string Inspector = "inspector";
    public const string Auditor = "auditor";
    public const string Administrator = "administrator";
}
