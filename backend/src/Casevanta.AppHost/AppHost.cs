using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL for the business modules. No module uses it yet; the resource exists so that the
// local topology matches the target architecture from the start.
var postgres = builder.AddPostgres("postgres");
var casevantaDb = postgres.AddDatabase("casevanta");

// The inherited OpenIddict server (https://localhost:44318). Keycloak replaces it in the next step.
var identityProvider = builder.AddProject<Projects.IdentityProvider>("identity-provider");

// The BFF and API host. Its launch profile keeps the public origin at https://localhost:5001,
// which the identity provider's registered redirect URIs depend on.
var api = builder.AddProject<Projects.Casevanta_Api>("api")
    .WithReference(casevantaDb)
    .WaitFor(identityProvider)
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
