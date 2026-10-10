using System.Net;
using Casevanta.Api.Tests.Infrastructure;
using Microsoft.Net.Http.Headers;

namespace Casevanta.Api.Tests;

/// <summary>Characterises the host page and the browser hardening headers (threat model T1, T2, T5, T9, T10).</summary>
[Collection(BffCollection.Name)]
public sealed class HostPageTests(BffFactory factory)
{
    [Fact]
    public async Task A05_Host_page_carries_a_fresh_nonce_that_matches_the_content_security_policy()
    {
        using var browser = factory.CreateBrowser();

        var first = await browser.GetAsync("/");
        var html = await first.BodyAsync();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.DoesNotContain("PLACEHOLDER_NONCE_SERVER", html);
        var nonce = ResponseAssertions.NonceFromHostPage(html);
        var csp = first.ContentSecurityPolicy();
        Assert.Contains($"'nonce-{nonce}'", csp["script-src"]);
        Assert.Contains($"'nonce-{nonce}'", csp["script-src-elem"]);
        Assert.Contains($"'nonce-{nonce}'", csp["style-src"]);
        Assert.Contains($"ngCspNonce=\"{nonce}\"", html);
        Assert.Contains($"<link nonce=\"{nonce}\" rel=\"stylesheet\"", html);
        Assert.Contains($"<script src=\"main.js\" type=\"module\" nonce=\"{nonce}\">", html);

        var secondNonce = ResponseAssertions.NonceFromHostPage(await (await browser.GetAsync("/")).BodyAsync());
        Assert.NotEqual(nonce, secondNonce);
    }

    [Fact]
    public async Task A06_Host_page_issues_the_antiforgery_cookie_pair_with_strict_attributes()
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.GetAsync("/");
        var cookies = TestBrowser.SetCookies(response).ToDictionary(cookie => cookie.Name.Value!);

        var cookieToken = cookies["__Host-X-XSRF-TOKEN"];
        Assert.True(cookieToken.HttpOnly);
        Assert.True(cookieToken.Secure);
        Assert.Equal(SameSiteMode.Strict, cookieToken.SameSite);
        Assert.Equal("/", cookieToken.Path.Value);

        var requestToken = cookies["XSRF-RequestToken"];
        Assert.False(requestToken.HttpOnly); // the SPA reads it and echoes it in the X-XSRF-TOKEN header
        Assert.True(requestToken.Secure);
        Assert.Equal(SameSiteMode.Strict, requestToken.SameSite);
    }

    [Fact]
    public async Task A07_Host_page_sends_the_browser_hardening_headers()
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.GetAsync("/");

        Assert.Equal("DENY", response.SingleHeader("X-Frame-Options"));
        Assert.Equal("nosniff", response.SingleHeader("X-Content-Type-Options"));
        Assert.Equal("strict-origin-when-cross-origin", response.SingleHeader("Referrer-Policy"));
        Assert.Equal("same-origin", response.SingleHeader("Cross-Origin-Opener-Policy"));
        Assert.Equal("same-origin", response.SingleHeader("Cross-Origin-Resource-Policy"));
        Assert.Equal("require-corp", response.SingleHeader("Cross-Origin-Embedder-Policy"));
        Assert.StartsWith("max-age=31536000", response.SingleHeader("Strict-Transport-Security"));
        Assert.False(string.IsNullOrWhiteSpace(response.SingleHeader("Permissions-Policy")));

        var csp = response.ContentSecurityPolicy();
        Assert.Equal(new[] { "'none'" }, csp["object-src"]);
        Assert.Equal(new[] { "'none'" }, csp["frame-ancestors"]);
        Assert.Equal(new[] { "'self'" }, csp["base-uri"]);
        Assert.Equal(new[] { "'self'", factory.IdentityProviderOrigin }, csp["form-action"]);
        Assert.True(csp.ContainsKey("block-all-mixed-content"));
    }

    [Theory]
    [InlineData("/cases/42/review")]
    [InlineData("/verification/queue")]
    public async Task A16_Client_side_routes_fall_back_to_the_host_page(string path)
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ngCspNonce=", await response.BodyAsync());
    }

    [Fact]
    public async Task A17_Api_responses_send_a_policy_that_forbids_active_content()
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.GetAsync("/api/User");

        var csp = response.ContentSecurityPolicy();
        Assert.Equal(new[] { "'none'" }, csp["script-src"]);
        Assert.Equal(new[] { "'none'" }, csp["frame-ancestors"]);
        Assert.Equal(new[] { "'none'" }, csp["form-action"]);
        Assert.Equal(new[] { "'script'" }, csp["require-trusted-types-for"]);
        Assert.Equal("DENY", response.SingleHeader("X-Frame-Options"));
        Assert.Contains("preload", response.SingleHeader("Strict-Transport-Security"));
    }
}
