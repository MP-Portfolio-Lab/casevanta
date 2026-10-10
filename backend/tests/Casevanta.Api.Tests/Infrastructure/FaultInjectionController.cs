using Microsoft.AspNetCore.Mvc;

namespace Casevanta.Api.Tests.Infrastructure;

/// <summary>
/// Test-only endpoint (added to the in-memory host as an application part) that fails the way a
/// real bug would, so the error contract can be tested without shipping a failing endpoint.
/// </summary>
[ApiController]
[Route("api/test-only")]
public sealed class FaultInjectionController : ControllerBase
{
    public const string SensitiveDetail = "Server=db;Password=do-not-leak";

    [HttpGet("unhandled")]
    public IActionResult Unhandled() => throw new InvalidOperationException(SensitiveDetail);
}
