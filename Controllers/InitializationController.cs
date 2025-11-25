using ADOTTA.Projects.Suite.Api.Configuration;
using ADOTTA.Projects.Suite.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ADOTTA.Projects.Suite.Api.Controllers;

[ApiController]
[Route("api/init")]
public class InitializationController : ControllerBase
{
    private readonly IInitializationService _initService;
    private readonly ILogger<InitializationController> _logger;
    private readonly ISAPServiceLayerClient _sapClient;
    private readonly SAPSettings _sapSettings;

    public InitializationController(
        IInitializationService initService,
        ILogger<InitializationController> logger,
        ISAPServiceLayerClient sapClient,
        IOptions<SAPSettings> sapOptions)
    {
        _initService = initService;
        _logger = logger;
        _sapClient = sapClient;
        _sapSettings = sapOptions.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Initialize()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_sapSettings.CompanyDB) ||
                string.IsNullOrWhiteSpace(_sapSettings.UserName) ||
                string.IsNullOrWhiteSpace(_sapSettings.Password))
            {
                return StatusCode(500, new { message = "Configurazione SAP incompleta in appsettings." });
            }

            var loginResponse = await _sapClient.LoginAsync(new LoginRequest
            {
                CompanyDB = _sapSettings.CompanyDB,
                UserName = _sapSettings.UserName,
                Password = _sapSettings.Password
            });

            if (string.IsNullOrWhiteSpace(loginResponse.SessionId))
            {
                return StatusCode(500, new { message = "Impossibile ottenere una sessione SAP valida." });
            }

            var result = await _initService.InitializeAsync(loginResponse.SessionId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialization failed");
            return StatusCode(500, new { message = "Initialization failed", error = ex.Message });
        }
    }
}


