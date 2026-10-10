using Microsoft.Extensions.Logging;

namespace Casevanta.AppHost.Tests.Infrastructure;

/// <summary>
/// Starts the real Casevanta.AppHost (containers and processes) once for all distributed tests.
/// Ports stay randomized so the tests can run next to a developer's own `aspire run`. The Angular
/// dev server is switched off: these tests need Docker and .NET, not Node.js.
/// </summary>
public sealed class AppHostFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);

    public DistributedApplication App { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Casevanta_AppHost>(
            ["--Frontend:Enabled=false"], cancellationToken);

        appHost.Services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        appHost.Services.ConfigureHttpClientDefaults(http =>
        {
            // The api serves the ASP.NET Core development certificate, which CI machines do not trust.
            http.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
                AllowAutoRedirect = false,
            });
        });

        App = await appHost.BuildAsync(cancellationToken).WaitAsync(StartupTimeout, cancellationToken);
        await App.StartAsync(cancellationToken).WaitAsync(StartupTimeout, cancellationToken);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("api", cancellationToken)
            .WaitAsync(StartupTimeout, cancellationToken);
    }

    public HttpClient CreateApiClient() => App.CreateHttpClient("api", "https");

    public async ValueTask DisposeAsync()
    {
        if (App is not null)
        {
            await App.DisposeAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class AppHostCollection : ICollectionFixture<AppHostFixture>
{
    public const string Name = "AppHost";
}
