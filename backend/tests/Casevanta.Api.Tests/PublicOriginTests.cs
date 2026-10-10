using System.Net;
using Casevanta.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;

namespace Casevanta.Api.Tests;

/// <summary>Redirect URIs come from configuration, not from the request (threat model T3, T8; ADR-0010).</summary>
public sealed class PublicOriginTests(PublicOriginBffFactory factory) : IClassFixture<PublicOriginBffFactory>
{
    [Fact]
    public async Task A22_Login_and_logout_use_the_configured_public_origin()
    {
        using var browser = factory.CreateBrowser();

        var login = await browser.GetAsync("/api/Account/Login");
        var loginQuery = QueryHelpers.ParseQuery(login.Headers.Location!.Query);
        Assert.Equal(new Uri(PublicOriginBffFactory.ConfiguredPublicOrigin, "/signin-oidc").ToString(), loginQuery["redirect_uri"].ToString());

        factory.SignIn(browser);
        await browser.GetAsync("/");
        var logout = await browser.PostAsync("/api/Account/Logout", request =>
            request.Content = new FormUrlEncodedContent([new("__RequestVerificationToken", browser.RequestToken)]));

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        var logoutQuery = QueryHelpers.ParseQuery(logout.Headers.Location!.Query);
        Assert.Equal(new Uri(PublicOriginBffFactory.ConfiguredPublicOrigin, "/signout-callback-oidc").ToString(), logoutQuery["post_logout_redirect_uri"].ToString());
    }
}
