using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Casevanta.AppHost.Tests.Infrastructure;

namespace Casevanta.AppHost.Tests;

/// <summary>Suite C: the BFF and Keycloak agree on the realm, the client, its secret and the redirect URIs.</summary>
[Collection(AppHostCollection.Name)]
public sealed class KeycloakTests(AppHostFixture fixture)
{
    private const string Realm = "casevanta-workforce";

    [Fact]
    public async Task C04_Keycloak_serves_the_workforce_realm_with_pkce()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.App.ResourceNotifications.WaitForResourceHealthyAsync("keycloak", cancellationToken)
            .WaitAsync(TimeSpan.FromMinutes(3), cancellationToken);
        using var keycloak = fixture.App.CreateHttpClient("keycloak", "http");

        var discovery = await keycloak.GetFromJsonAsync<JsonElement>($"/realms/{Realm}/.well-known/openid-configuration", cancellationToken);

        Assert.EndsWith($"/realms/{Realm}", discovery.GetProperty("issuer").GetString());
        Assert.Contains("S256", discovery.GetProperty("code_challenge_methods_supported").EnumerateArray().Select(method => method.GetString()));
    }

    [Fact]
    public async Task C05_Login_through_the_api_reaches_the_keycloak_login_page()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var api = fixture.CreateApiClient();

        var challenge = await api.GetAsync("/api/Account/Login", cancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        var location = challenge.Headers.Location!;
        Assert.EndsWith($"/realms/{Realm}/protocol/openid-connect/auth", location.GetLeftPart(UriPartial.Path));
        var query = HttpUtility.ParseQueryString(location.Query);
        Assert.Equal("staff-web", query["client_id"]);
        // Pushed authorization request: the BFF already sent the parameters (and authenticated with the
        // client secret) over the back channel, so the browser only carries a request_uri.
        Assert.StartsWith("urn:ietf:params:oauth:request_uri:", query["request_uri"]);

        // Follow Keycloak's own redirects (it moves the browser to login-actions with a session cookie).
        using var browser = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            CookieContainer = new CookieContainer(),
        });
        var loginPage = await browser.GetAsync(location, cancellationToken);
        var html = await loginPage.Content.ReadAsStringAsync(cancellationToken);

        Assert.True(loginPage.StatusCode == HttpStatusCode.OK,
            $"Keycloak answered {(int)loginPage.StatusCode} at {loginPage.RequestMessage?.RequestUri} instead of its login page.");
        Assert.Contains("name=\"username\"", html);
        Assert.Contains("name=\"password\"", html);
    }

    [Fact]
    public async Task C06_The_verifier_endpoint_rejects_anonymous_callers()
    {
        using var api = fixture.CreateApiClient();

        var response = await api.GetAsync("/api/verification/ping", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
