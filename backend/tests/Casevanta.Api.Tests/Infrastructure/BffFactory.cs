using System.Security.Claims;
using Casevanta.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Casevanta.Api.Tests.Infrastructure;

/// <summary>
/// Hosts the BFF in memory (environment "Test") with a static OpenID Connect configuration, so no
/// identity provider is contacted. The identity provider endpoints are derived from the configured
/// authority, which keeps these tests valid when the identity provider changes.
/// </summary>
public sealed class BffFactory : WebApplicationFactory<UserInfo>
{
    /// <summary>
    /// The origin of every request the in-memory client sends. It is deliberately not localhost:
    /// HSTS is never sent to localhost, and these tests characterise deployed behaviour.
    /// </summary>
    public static readonly Uri PublicOrigin = new("https://staff.casevanta.test");

    /// <summary>The ID token stored in forged session tickets; logout must send it as id_token_hint.</summary>
    public const string TestIdToken = "test-id-token";

    static BffFactory()
    {
        // Outside Development, Pages/_Host.cshtml reads wwwroot/index.html relative to the process
        // working directory. The test project copies a fixture to that location.
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureTestServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                var authority = (options.Authority
                    ?? throw new InvalidOperationException("The OpenID Connect authority is not configured.")).TrimEnd('/');

                options.Configuration = new OpenIdConnectConfiguration
                {
                    Issuer = authority,
                    AuthorizationEndpoint = $"{authority}/test/authorize",
                    TokenEndpoint = $"{authority}/test/token",
                    EndSessionEndpoint = $"{authority}/test/end-session",
                };
            });
        });
    }

    public OpenIdConnectOptions OidcOptions =>
        Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>().Get(OpenIdConnectDefaults.AuthenticationScheme);

    /// <summary>Scheme, host and port of the configured identity provider.</summary>
    public string IdentityProviderOrigin =>
        new Uri(OidcOptions.Authority!).GetLeftPart(UriPartial.Authority);

    public TestBrowser CreateBrowser() => new(CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = PublicOrigin,
        AllowAutoRedirect = false,
        HandleCookies = false,
    }));

    /// <summary>
    /// Creates a session cookie value with the application's own ticket format, as if the OpenID
    /// Connect sign-in had completed. Name and role claim types follow the configured token
    /// validation parameters.
    /// </summary>
    public string CreateSessionCookieValue(string name, string subject, params string[] roles)
    {
        var validation = OidcOptions.TokenValidationParameters;
        var claims = new List<Claim> { new("sub", subject), new(validation.NameClaimType, name) };
        claims.AddRange(roles.Select(role => new Claim(validation.RoleClaimType, role)));

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: OpenIdConnectDefaults.AuthenticationScheme,
            nameType: validation.NameClaimType,
            roleType: validation.RoleClaimType);

        var properties = new AuthenticationProperties();
        properties.StoreTokens([new AuthenticationToken { Name = "id_token", Value = TestIdToken }]);

        var cookieOptions = Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(CookieAuthenticationDefaults.AuthenticationScheme);

        return cookieOptions.TicketDataFormat.Protect(
            new AuthenticationTicket(new ClaimsPrincipal(identity), properties, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    /// <summary>Signs a browser in by planting a forged session cookie.</summary>
    public void SignIn(TestBrowser browser, string name = "Test Employee", string subject = "test-user-1", params string[] roles) =>
        browser.SetCookie(SessionCookieName, CreateSessionCookieValue(name, subject, roles));

    public const string SessionCookieName = ".AspNetCore.Cookies";
}
