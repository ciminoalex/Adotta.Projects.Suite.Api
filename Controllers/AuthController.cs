using ADOTTA.Projects.Suite.Api.Configuration;
using ADOTTA.Projects.Suite.Api.DTOs;
using ADOTTA.Projects.Suite.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ADOTTA.Projects.Suite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISAPServiceLayerClient _sapClient;
    private readonly IUserService _userService;
    private readonly ILogger<AuthController> _logger;
    private readonly SAPSettings _sapSettings;

    public AuthController(
        ISAPServiceLayerClient sapClient,
        IUserService userService,
        ILogger<AuthController> logger,
        IOptions<SAPSettings> sapSettings)
    {
        _sapClient = sapClient;
        _userService = userService;
        _logger = logger;
        _sapSettings = sapSettings.Value;
    }

    private string GetSessionId() => Request.Headers["X-SAP-Session-Id"].ToString() ?? string.Empty;

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            // Se CompanyDB non è fornito, usa quello dalla configurazione
            var companyDB = string.IsNullOrWhiteSpace(request.CompanyDB) 
                ? _sapSettings.CompanyDB 
                : request.CompanyDB;

            var loginRequest = new LoginRequest
            {
                CompanyDB = companyDB,
                UserName = request.UserName,
                Password = request.Password
            };

            var response = await _sapClient.LoginAsync(loginRequest);

            if (!string.IsNullOrWhiteSpace(response.SessionId))
            {
                Response.Headers["X-SAP-Session-Id"] = response.SessionId;
            }
            
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

    [HttpGet("users/by-email/{email}")]
    public async Task<ActionResult<UserDto>> GetUserByEmail(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { message = "Email is required" });
            }

            var user = await _userService.GetUserByEmailAsync(email, GetSessionId());
            if (user == null)
            {
                return NotFound(new { message = $"User with email '{email}' not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email {Email}", email);
            return StatusCode(500, new { message = "Error retrieving user", error = ex.Message });
        }
    }
}

