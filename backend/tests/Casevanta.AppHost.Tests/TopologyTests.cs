using System.Net.Http.Json;
using Casevanta.AppHost.Tests.Infrastructure;

namespace Casevanta.AppHost.Tests;

/// <summary>Suite C: the distributed application starts, reports health and serves its contract.</summary>
[Collection(AppHostCollection.Name)]
public sealed class TopologyTests(AppHostFixture fixture)
{
    [Fact]
    public async Task C01_The_api_reports_ready_and_alive()
    {
        using var client = fixture.CreateApiClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health", cancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/alive", cancellationToken)).StatusCode);
    }

    [Fact]
    public async Task C02_The_api_serves_the_anonymous_user_contract()
    {
        using var client = fixture.CreateApiClient();

        var user = await client.GetFromJsonAsync<UserInfo>("/api/User", TestContext.Current.CancellationToken);

        Assert.NotNull(user);
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public async Task C03_The_business_database_is_provisioned_and_healthy()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await fixture.App.ResourceNotifications.WaitForResourceHealthyAsync("casevanta", cancellationToken)
            .WaitAsync(TimeSpan.FromMinutes(2), cancellationToken);

        Assert.False(string.IsNullOrEmpty(await fixture.App.GetConnectionStringAsync("casevanta", cancellationToken)));
    }

    [Fact]
    public async Task C07_The_openapi_document_describes_the_api_in_development()
    {
        using var client = fixture.CreateApiClient();

        var document = await client.GetFromJsonAsync<System.Text.Json.JsonElement>("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.StartsWith("3.", document.GetProperty("openapi").GetString());
        var paths = document.GetProperty("paths").EnumerateObject().Select(path => path.Name).ToArray();
        Assert.Contains("/api/User", paths);
        Assert.Contains("/api/verification/ping", paths);
    }

    private sealed record UserInfo(bool IsAuthenticated);
}
