using Casevanta.Api;
using Casevanta.Api.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry, health checks, service discovery and HTTP resilience (Casevanta.ServiceDefaults).
builder.AddServiceDefaults();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
});

var services = builder.Services;
var configuration = builder.Configuration;

var identityProviderAuthority = configuration["OpenIDConnectSettings:Authority"];
if (string.IsNullOrWhiteSpace(identityProviderAuthority))
{
    throw new InvalidOperationException(
        "OpenIDConnectSettings:Authority is not configured. Start the application through the Aspire AppHost "
        + "(scripts/dev.sh) or provide the setting; see docs/architecture/configuration.md.");
}

// CSP form-action must allow the identity provider's origin: the logout form posts to the BFF, which
// redirects to the provider's end-session endpoint. A source with a path would only match that path.
var identityProviderOrigin = new Uri(identityProviderAuthority).GetLeftPart(UriPartial.Authority);

services.AddSecurityHeaderPolicies()
    .SetPolicySelector(ctx =>
    {
        if (ctx.HttpContext.Request.Path.StartsWithSegments("/api"))
        {
            return ApiSecurityHeadersDefinitions.GetHeaderPolicyCollection(builder.Environment.IsDevelopment());
        }

        return SecurityHeadersDefinitions.GetHeaderPolicyCollection(
            builder.Environment.IsDevelopment(), identityProviderOrigin);
    });

services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "__Host-X-XSRF-TOKEN";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

services.AddHttpClient();
services.AddOptions();

// Session cookies stay valid across restarts and instances of the same application only if they
// share keys; key storage for deployed environments is described in ADR-0008.
services.AddDataProtection().SetApplicationName("casevanta");

services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    // A working day; activity extends it. Tokens are not refreshed yet (threat model G3).
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddOpenIdConnect(options =>
{
    configuration.GetSection("OpenIDConnectSettings").Bind(options);

    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.ResponseType = OpenIdConnectResponseType.Code;
    // Metadata and tokens only over HTTPS, in every environment (the development Keycloak serves HTTPS too).
    options.RequireHttpsMetadata = true;
    options.Scope.Add("email");

    // Tokens stay server-side in the encrypted session; this also makes logout send id_token_hint.
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "name",
        RoleClaimType = "roles",
    };

    options.UsePublicOrigin();
});

services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.Verifier, policy => policy
        // API callers get 401/403 from the cookie scheme instead of a redirect to the identity provider.
        .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .RequireRole(Roles.Verifier));

services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

services.AddRazorPages().AddMvcOptions(options =>
{
    //var policy = new AuthorizationPolicyBuilder()
    //    .RequireAuthenticatedUser()
    //    .Build();
    //options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddReverseProxy()
   .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

if (app.Environment.IsDevelopment())
{
    // Personal data in identity model logs helps local debugging; never enable it elsewhere.
    IdentityModelEventSource.ShowPII = true;
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseSecurityHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseNoUnauthorizedRedirect("/api");

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapDefaultEndpoints();
app.MapNotFound("/api/{**segment}");

if (app.Environment.IsDevelopment())
{
    var uiDevServer = app.Configuration.GetValue<string>("UiDevServerUrl");
    if (!string.IsNullOrEmpty(uiDevServer))
    {
        app.MapReverseProxy();
    }
}

app.MapFallbackToPage("/_Host");

app.Run();
