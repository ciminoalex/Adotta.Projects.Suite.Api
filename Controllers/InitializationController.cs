using ADOTTA.Projects.Suite.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ADOTTA.Projects.Suite.Api.Extensions;

namespace ADOTTA.Projects.Suite.Api.Controllers;

[ApiController]
[Route("api/init")]
[Authorize]
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
        var sessionId = HttpContext.GetSapSessionId();
        if (string.IsNullOrEmpty(sessionId))
        {
            return BadRequest("Sessione SAP non disponibile");
        }

        var result = await _initService.InitializeAsync(sessionId);
        return Ok(result);
    }
}


