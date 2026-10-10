using System.Net;
using Casevanta.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Casevanta.Api.Tests;

/// <summary>Characterises the OpenID Connect login and logout exchange (threat model T3, T4, T6, T8).</summary>
[Collection(BffCollection.Name)]
public sealed class LoginLogoutTests(BffFactory factory)
{
    [Fact]
    public async Task A04_Login_redirects_to_the_identity_provider_with_code_flow_pkce_state_and_nonce()
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.GetAsync("/api/Account/Login");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal(factory.OidcOptions.Configuration!.AuthorizationEndpoint, location.GetLeftPart(UriPartial.Path));

        var query = QueryHelpers.ParseQuery(location.Query);
        Assert.Equal(factory.OidcOptions.ClientId, Value(query, "client_id"));
        Assert.Equal("code", Value(query, "response_type"));
        Assert.Equal("S256", Value(query, "code_challenge_method"));
        Assert.NotEmpty(Value(query, "code_challenge"));
        Assert.NotEmpty(Value(query, "state"));
        Assert.NotEmpty(Value(query, "nonce"));
        Assert.Equal(new Uri(BffFactory.PublicOrigin, "/signin-oidc").ToString(), Value(query, "redirect_uri"));
        var scopes = Value(query, "scope").Split(' ');
        Assert.Contains("openid", scopes);
        Assert.Contains("profile", scopes);
        Assert.Contains("email", scopes);

        // The correlation and nonce cookies protect the callback; scripts must not read them.
        var protocolCookies = TestBrowser.SetCookies(response)
            .Where(cookie => cookie.Name.Value!.StartsWith(".AspNetCore.Correlation.", StringComparison.Ordinal)
                || cookie.Name.Value!.StartsWith(".AspNetCore.OpenIdConnect.Nonce.", StringComparison.Ordinal))
            .ToList();
        Assert.Equal(2, protocolCookies.Count);
        Assert.All(protocolCookies, cookie =>
        {
            Assert.True(cookie.HttpOnly);
            Assert.True(cookie.Secure);
        });
    }

    [Theory]
    [InlineData(null, "/")]
    [InlineData("/cases/42", "/cases/42")]
    [InlineData("cases/42", "/cases/42")]
    [InlineData("https://evil.example/steal?x=1", "/steal?x=1")]
    public async Task A15_Login_keeps_the_post_login_redirect_on_this_site(string? returnUrl, string expectedRedirect)
    {
        using var browser = factory.CreateBrowser();
        var path = returnUrl is null ? "/api/Account/Login" : $"/api/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}";

        var response = await browser.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(expectedRedirect, RedirectAfterLogin(response));
    }

    [Theory]
    [InlineData("//evil.example/steal")]
    [InlineData("/\\evil.example/steal")]
    [InlineData("\\\\evil.example/steal")]
    [InlineData("https://evil.example//other.example/steal")]
    [InlineData("/\t/evil.example/steal")]
    public async Task A20_Login_falls_back_to_the_root_for_return_urls_that_would_leave_the_site(string returnUrl)
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.GetAsync($"/api/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", RedirectAfterLogin(response));
    }

    [Fact]
    public async Task A11_Logout_clears_the_session_and_ends_the_identity_provider_session()
    {
        using var browser = factory.CreateBrowser();
        factory.SignIn(browser);
        await browser.GetAsync("/"); // the logout form posts the request token, as the SPA does

        var response = await browser.PostAsync("/api/Account/Logout", request =>
            request.Content = new FormUrlEncodedContent([new("__RequestVerificationToken", browser.RequestToken)]));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal(factory.OidcOptions.Configuration!.EndSessionEndpoint, location.GetLeftPart(UriPartial.Path));
        var query = QueryHelpers.ParseQuery(location.Query);
        Assert.Equal(BffFactory.TestIdToken, Value(query, "id_token_hint"));
        Assert.Equal(new Uri(BffFactory.PublicOrigin, "/signout-callback-oidc").ToString(), Value(query, "post_logout_redirect_uri"));
        Assert.NotEmpty(Value(query, "state"));

        Assert.False(browser.Cookies.ContainsKey(BffFactory.SessionCookieName));
        var user = await (await browser.GetAsync("/api/User")).JsonAsync<UserInfoResponse>();
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public async Task A12_Logout_rejects_requests_without_an_antiforgery_token()
    {
        // Gap G2 closed: another site cannot force a logout by posting to this endpoint.
        using var browser = factory.CreateBrowser();
        factory.SignIn(browser);
        await browser.GetAsync("/");

        var response = await browser.PostAsync("/api/Account/Logout");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(browser.Cookies.ContainsKey(BffFactory.SessionCookieName), "The session must survive a rejected logout.");
    }

    [Fact]
    public async Task A18_Anonymous_logout_is_challenged_with_a_login_redirect()
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.PostAsync("/api/Account/Logout");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(factory.OidcOptions.Configuration!.AuthorizationEndpoint, response.Headers.Location!.GetLeftPart(UriPartial.Path));
    }

    private string RedirectAfterLogin(HttpResponseMessage challenge)
    {
        var state = Value(QueryHelpers.ParseQuery(challenge.Headers.Location!.Query), "state");
        var properties = factory.OidcOptions.StateDataFormat.Unprotect(state);
        Assert.NotNull(properties);
        return properties.RedirectUri!;
    }

    private static string Value(Dictionary<string, StringValues> query, string key) =>
        query.TryGetValue(key, out var value) ? value.ToString() : string.Empty;
}
