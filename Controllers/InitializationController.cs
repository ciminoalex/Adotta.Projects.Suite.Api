using ADOTTA.Projects.Suite.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ADOTTA.Projects.Suite.Api.Controllers;

[ApiController]
[Route("api/init")]
public class InitializationController : ControllerBase
{
    private readonly IInitializationService _initService;
    private readonly ILogger<InitializationController> _logger;

    public InitializationController(IInitializationService initService, ILogger<InitializationController> logger)
    {
        _initService = initService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Initialize()
    {
        var sessionId = HttpContext.Items["SAPSessionId"] as string ?? string.Empty;
        if (string.IsNullOrEmpty(sessionId))
        {
            return BadRequest("Missing X-SAP-Session-Id header");
        }

        var result = await _initService.InitializeAsync(sessionId);
        return Ok(result);
    }
}


