using Microsoft.AspNetCore.Hosting;

namespace Casevanta.Api.Tests.Infrastructure;

/// <summary>
/// The BFF behind a reverse proxy or a test harness with randomized ports: App:PublicOrigin must
/// decide the redirect URIs it sends to the identity provider, not the address it happens to run on.
/// </summary>
public sealed class PublicOriginBffFactory : BffFactory
{
    public static readonly Uri ConfiguredPublicOrigin = new("https://casevanta.example");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("App:PublicOrigin", ConfiguredPublicOrigin.ToString());
    }
}
