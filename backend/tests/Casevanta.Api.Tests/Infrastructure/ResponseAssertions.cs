using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Casevanta.Api.Tests.Infrastructure;

public static partial class ResponseAssertions
{
    /// <summary>Parses the Content-Security-Policy header into directive → sources.</summary>
    public static IReadOnlyDictionary<string, string[]> ContentSecurityPolicy(this HttpResponseMessage response)
    {
        var header = Assert.Single(response.Headers.GetValues("Content-Security-Policy"));
        return header
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(directive => directive.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToDictionary(parts => parts[0], parts => parts[1..], StringComparer.OrdinalIgnoreCase);
    }

    public static string SingleHeader(this HttpResponseMessage response, string name) =>
        Assert.Single(response.Headers.GetValues(name));

    public static Task<string> BodyAsync(this HttpResponseMessage response) =>
        response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

    public static async Task<T> JsonAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(TestContext.Current.CancellationToken))!;

    public static string NonceFromHostPage(string html)
    {
        var match = CspNonceMeta().Match(html);
        Assert.True(match.Success, "The host page has no CSP_NONCE meta tag.");
        return match.Groups["nonce"].Value;
    }

    [GeneratedRegex("<meta name=\"CSP_NONCE\" content=\"(?<nonce>[^\"]+)\"")]
    private static partial Regex CspNonceMeta();
}

/// <summary>The JSON contract of GET /api/User that the SPA consumes.</summary>
public sealed record UserInfoResponse(bool IsAuthenticated, string NameClaimType, string RoleClaimType, ClaimResponse[] Claims);

public sealed record ClaimResponse(string Type, string Value);
