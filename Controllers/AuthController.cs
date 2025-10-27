using ADOTTA.Projects.Suite.Api.DTOs;
using ADOTTA.Projects.Suite.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ADOTTA.Projects.Suite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISAPServiceLayerClient _sapClient;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ISAPServiceLayerClient sapClient, ILogger<AuthController> logger)
    {
        _sapClient = sapClient;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var loginRequest = new LoginRequest
            {
                CompanyDB = request.CompanyDB,
                UserName = request.UserName,
                Password = request.Password,
                Language = request.Language
            };

            var response = await _sapClient.LoginAsync(loginRequest);
            
            return Ok(new LoginResponseDto
            {
                SessionId = response.SessionId,
                Version = response.Version,
                SessionTimeout = response.SessionTimeout
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "Login failed", error = ex.Message });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromHeader(Name = "X-SAP-Session-Id")] string sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new { message = "SessionId is required" });
            }

            await _sapClient.LogoutAsync(sessionId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "Logout failed", error = ex.Message });
        }
    }
}

