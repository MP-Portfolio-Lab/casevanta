using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Casevanta.Api.Services;

/// <summary>
/// Builds the OpenID Connect redirect URIs from App:PublicOrigin when it is configured, so they do
/// not depend on the address the BFF happens to run on (a reverse proxy, randomized test ports).
/// Without the setting, the request's own origin is used. The value is an origin only, with no path.
/// </summary>
public static class PublicOriginExtensions
{
    public const string PublicOriginKey = "App:PublicOrigin";

    public static void UsePublicOrigin(this OpenIdConnectOptions options)
    {
        var redirectToIdentityProvider = options.Events.OnRedirectToIdentityProvider;
        options.Events.OnRedirectToIdentityProvider = async context =>
        {
            if (PublicOrigin(context.HttpContext) is { } origin)
            {
                context.ProtocolMessage.RedirectUri = new Uri(origin, context.Options.CallbackPath.Value).ToString();
            }

            await redirectToIdentityProvider(context);
        };

        var redirectToIdentityProviderForSignOut = options.Events.OnRedirectToIdentityProviderForSignOut;
        options.Events.OnRedirectToIdentityProviderForSignOut = async context =>
        {
            if (PublicOrigin(context.HttpContext) is { } origin)
            {
                context.ProtocolMessage.PostLogoutRedirectUri = new Uri(origin, context.Options.SignedOutCallbackPath.Value).ToString();
            }

            await redirectToIdentityProviderForSignOut(context);
        };
    }

    private static Uri? PublicOrigin(HttpContext httpContext) =>
        httpContext.RequestServices.GetRequiredService<IConfiguration>()[PublicOriginKey] is { Length: > 0 } value
            ? new Uri(value, UriKind.Absolute)
            : null;
}
