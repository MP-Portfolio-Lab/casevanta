using System.Net;
using Casevanta.Api.Tests.Infrastructure;

namespace Casevanta.Api.Tests;

/// <summary>Characterises signed-in API access and the antiforgery protection (threat model T2, T7).</summary>
[Collection(BffCollection.Name)]
public sealed class AuthenticatedApiTests(BffFactory factory)
{
    [Fact]
    public async Task A08_User_endpoint_returns_the_signed_in_user_name()
    {
        using var browser = factory.CreateBrowser();
        factory.SignIn(browser, name: "Ada Employee");

        var user = await (await browser.GetAsync("/api/User")).JsonAsync<UserInfoResponse>();

        Assert.True(user.IsAuthenticated);
        Assert.Equal("name", user.NameClaimType);
        Assert.Contains(user.Claims, claim => claim.Type == "name" && claim.Value == "Ada Employee");
    }

    [Fact]
    public async Task A09_Protected_api_accepts_a_signed_in_call_that_carries_the_antiforgery_header()
    {
        using var browser = factory.CreateBrowser();
        factory.SignIn(browser);
        await browser.GetAsync("/"); // the host page issues antiforgery tokens bound to this user

        var response = await browser.GetAsync("/api/DirectApi", request => request.Headers.Add("X-XSRF-TOKEN", browser.RequestToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new[] { "some data", "more data", "loads of data" }, await response.JsonAsync<string[]>());
    }

    [Fact]
    public async Task A10_Protected_api_rejects_a_signed_in_call_without_the_antiforgery_header()
    {
        using var browser = factory.CreateBrowser();
        factory.SignIn(browser);
        await browser.GetAsync("/");

        var response = await browser.GetAsync("/api/DirectApi");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A19_Protected_api_rejects_antiforgery_tokens_issued_to_another_user()
    {
        using var victim = factory.CreateBrowser();
        factory.SignIn(victim, name: "Victim", subject: "user-victim");
        await victim.GetAsync("/");

        using var attacker = factory.CreateBrowser();
        factory.SignIn(attacker, name: "Attacker", subject: "user-attacker");
        attacker.SetCookie("__Host-X-XSRF-TOKEN", victim.Cookies["__Host-X-XSRF-TOKEN"]);

        var response = await attacker.GetAsync("/api/DirectApi", request => request.Headers.Add("X-XSRF-TOKEN", victim.RequestToken));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
