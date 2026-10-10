using Casevanta.Api.Tests.Infrastructure;
using Microsoft.IdentityModel.Logging;

namespace Casevanta.Api.Tests;

/// <summary>Diagnostics must not leak personal data outside Development (threat model T9, gap G4).</summary>
[Collection(BffCollection.Name)]
public sealed class DiagnosticsTests(BffFactory factory)
{
    [Fact]
    public void A21_Identity_model_logs_hide_personal_data_outside_development()
    {
        _ = factory.Services; // starts the host, which runs the application's startup code

        Assert.False(IdentityModelEventSource.ShowPII);
    }
}
