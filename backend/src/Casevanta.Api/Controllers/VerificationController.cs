using Casevanta.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Casevanta.Api.Controllers;

/// <summary>
/// Proves the verifier policy end to end until the verification module exists: anonymous callers get
/// 401, signed-in users without the verifier role get 403.
/// </summary>
[ApiController]
[Route("api/verification")]
[Authorize(Policy = AuthorizationPolicies.Verifier)]
public class VerificationController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { Status = "ok", Verifier = User.Identity?.Name });
}
