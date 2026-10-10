using System.Net;
using Casevanta.Api.Tests.Infrastructure;

namespace Casevanta.Api.Tests;

/// <summary>Characterises what an anonymous visitor can reach (threat model T7, T9).</summary>
[Collection(BffCollection.Name)]
public sealed class AnonymousAccessTests(BffFactory factory)
{
    [Fact]
    public async Task A01_User_endpoint_reports_an_anonymous_visitor()
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.GetAsync("/api/User");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.JsonAsync<UserInfoResponse>();
        Assert.False(user.IsAuthenticated);
        Assert.Empty(user.Claims);
    }

    [Fact]
    public async Task A02_Protected_api_rejects_anonymous_calls_with_401_instead_of_a_login_redirect()
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.GetAsync("/api/DirectApi");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task A03_Unknown_api_routes_return_404_instead_of_the_host_page()
    {
        using var browser = factory.CreateBrowser();

        var response = await browser.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("<html", await response.BodyAsync(), StringComparison.OrdinalIgnoreCase);
    }
}
