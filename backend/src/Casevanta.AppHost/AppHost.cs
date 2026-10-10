using Microsoft.Extensions.Configuration;

const string WorkforceRealm = "casevanta-workforce";
const string PublicOrigin = "https://localhost:5001";

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL for the business modules. No module uses it yet; the resource exists so that the
// local topology matches the target architecture from the start.
var postgres = builder.AddPostgres("postgres");
var casevantaDb = postgres.AddDatabase("casevanta");

// Secret of the staff-web OIDC client: generated once per machine, kept in the AppHost's user
// secrets, and handed to both Keycloak (realm import placeholder) and the BFF.
var staffWebClientSecret = builder.AddParameter(
    "staff-web-client-secret",
    new GenerateParameterDefault { MinLength = 32, Special = false },
    secret: true,
    persist: true);

// Keycloak with the workforce realm (infra/keycloak/realms), served over HTTPS with the ASP.NET Core
// development certificate. No data volume: every start re-imports the realm, so the JSON stays the
// single source of development users and clients. The fixed port keeps the issuer and the browser's
// Keycloak cookies stable across restarts.
var keycloak = builder.AddKeycloak("keycloak", port: 8080)
    .WithRealmImport("../../../infra/keycloak/realms")
    .WithEnvironment("STAFF_WEB_CLIENT_SECRET", staffWebClientSecret)
    .WithOtlpExporter();

// The BFF and API host. App:PublicOrigin pins the redirect URIs registered in Keycloak, even when
// integration tests run the api on a randomized port.
var api = builder.AddProject<Projects.Casevanta_Api>("api")
    .WithReference(casevantaDb)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    // The Keycloak endpoint is named "http" but serves HTTPS once the development certificate is applied.
    .WithEnvironment("OpenIDConnectSettings__Authority", ReferenceExpression.Create($"{keycloak.GetEndpoint("http")}/realms/{WorkforceRealm}"))
    .WithEnvironment("OpenIDConnectSettings__ClientSecret", staffWebClientSecret)
    .WithEnvironment("App__PublicOrigin", PublicOrigin)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

// The Angular dev server, proxied by the BFF in Development. Integration tests pass
// --Frontend:Enabled=false so that they need neither Node.js nor pnpm.
if (builder.Configuration.GetValue("Frontend:Enabled", defaultValue: true))
{
    var staffWeb = builder.AddJavaScriptApp("staff-web", "../../..", runScriptName: "staff-web:serve")
        .WithPnpm(installArgs: ["--frozen-lockfile"])
        .WithHttpEndpoint(port: 4201, env: "PORT")
        .WithEnvironment("NX_DAEMON", "false")
        .WithHttpHealthCheck("/");

    api.WithReference(staffWeb)
        .WaitFor(staffWeb)
        .WithEnvironment("UiDevServerUrl", staffWeb.GetEndpoint("http"))
        .WithEnvironment("ReverseProxy__Clusters__staff-web__Destinations__dev-server__Address", staffWeb.GetEndpoint("http"));
}

builder.Build().Run();
