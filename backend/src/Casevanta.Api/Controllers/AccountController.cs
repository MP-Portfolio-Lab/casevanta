using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Casevanta.Api.Controllers;

[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    [HttpGet("Login")]
    public ActionResult Login(string? returnUrl, string? claimsChallenge)
    {
        var properties = GetAuthProperties(returnUrl);

        if (claimsChallenge != null)
        {
            string jsonString = claimsChallenge.Replace("\\", "").Trim('"');

            properties.Items["claims"] = jsonString;
        }

        return Challenge(properties);
    }

    // [ValidateAntiForgeryToken] // not needed explicitly due the the Auto global definition.
    [IgnoreAntiforgeryToken] // need to apply this to the form post request
    [Authorize]
    [HttpPost("Logout")]
    public IActionResult Logout()
    {
        return SignOut(
            new AuthenticationProperties { RedirectUri = "/" },
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    private static AuthenticationProperties GetAuthProperties(string? returnUrl) =>
        new() { RedirectUri = ToLocalRedirect(returnUrl) };

    /// <summary>
    /// Reduces a requested return URL to a path on this site, which prevents open redirects.
    /// Absolute http(s) URLs keep only their path and query, relative paths are anchored at the
    /// root, and anything that could still leave the site (protocol-relative or backslash forms,
    /// control characters that browsers strip) falls back to the root.
    /// </summary>
    internal static string ToLocalRedirect(string? returnUrl)
    {
        const string root = "/";

        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return root;
        }

        var candidate =
            Uri.TryCreate(returnUrl, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttps || absolute.Scheme == Uri.UriSchemeHttp)
                ? absolute.PathAndQuery
                : returnUrl.StartsWith('/') ? returnUrl : root + returnUrl;

        return IsLocalPath(candidate) ? candidate : root;
    }

    private static bool IsLocalPath(string path) =>
        path.StartsWith('/')
        && (path.Length == 1 || (path[1] != '/' && path[1] != '\\'))
        && !path.Any(char.IsControl);
}
