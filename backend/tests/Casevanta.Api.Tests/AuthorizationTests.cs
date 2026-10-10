using System.Net;
using Casevanta.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Casevanta.Api.Tests;

/// <summary>Role-based policies and session policy (threat model T6, T7; ADR-0006, ADR-0008).</summary>
[Collection(BffCollection.Name)]
public sealed class AuthorizationTests(BffFactory factory)
{
    [Fact]
    public async Task A23_Verification_ping_requires_the_verifier_role()
    {
        using var anonymous = factory.CreateBrowser();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/verification/ping")).StatusCode);

        using var employee = factory.CreateBrowser();
        factory.SignIn(employee, name: "Ada Employee", subject: "user-employee", "employee");
        Assert.Equal(HttpStatusCode.Forbidden, (await employee.GetAsync("/api/verification/ping")).StatusCode);

        using var verifier = factory.CreateBrowser();
        factory.SignIn(verifier, name: "Victor Verifier", subject: "user-verifier", "employee", "verifier");
        Assert.Equal(HttpStatusCode.OK, (await verifier.GetAsync("/api/verification/ping")).StatusCode);
    }

    [Fact]
    public async Task A24_User_endpoint_exposes_the_roles_of_the_signed_in_user()
    {
        using var browser = factory.CreateBrowser();
        factory.SignIn(browser, name: "Victor Verifier", subject: "user-verifier", "employee", "verifier");

        var user = await (await browser.GetAsync("/api/User")).JsonAsync<UserInfoResponse>();

        Assert.Equal("roles", user.RoleClaimType);
        Assert.Equal(
            new[] { "employee", "verifier" },
            user.Claims.Where(claim => claim.Type == user.RoleClaimType).Select(claim => claim.Value).Order());
    }

    [Fact]
    public void A25_Sessions_are_bounded_and_survive_restarts_of_the_same_application()
    {
        var cookie = factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);
        Assert.Equal(TimeSpan.FromHours(8), cookie.ExpireTimeSpan);
        Assert.True(cookie.SlidingExpiration);
        Assert.Equal(CookieSecurePolicy.Always, cookie.Cookie.SecurePolicy);
        Assert.True(cookie.Cookie.HttpOnly);

        var dataProtection = factory.Services.GetRequiredService<IOptions<DataProtectionOptions>>().Value;
        Assert.Equal("casevanta", dataProtection.ApplicationDiscriminator);
    }
}
