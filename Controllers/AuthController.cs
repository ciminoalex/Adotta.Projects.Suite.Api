using ADOTTA.Projects.Suite.Api.Configuration;
using ADOTTA.Projects.Suite.Api.DTOs;
using ADOTTA.Projects.Suite.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ADOTTA.Projects.Suite.Api.Extensions;
using Microsoft.AspNetCore.Authorization;

namespace ADOTTA.Projects.Suite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISAPServiceLayerClient _sapClient;
    private readonly IUserService _userService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;
    private readonly SAPSettings _sapSettings;

    public AuthController(
        ISAPServiceLayerClient sapClient,
        IUserService userService,
        IJwtTokenService jwtTokenService,
        ILogger<AuthController> logger,
        IOptions<SAPSettings> sapSettings)
    {
        _sapClient = sapClient;
        _userService = userService;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
        _sapSettings = sapSettings.Value;
    }

    private string GetSessionId() => HttpContext.GetSapSessionId();

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Email e password sono obbligatorie" });
            }

            if (string.IsNullOrWhiteSpace(_sapSettings.Password))
            {
                _logger.LogError("SAP password is not configured. Cannot complete login.");
                return StatusCode(500, new { message = "Configurazione SAP non valida" });
            }

            var loginRequest = new LoginRequest
            {
                CompanyDB = _sapSettings.CompanyDB,
                UserName = _sapSettings.UserName,
                Password = _sapSettings.Password
            };

            var response = await _sapClient.LoginAsync(loginRequest);

            var user = await _userService.GetUserByEmailAsync(request.Email, response.SessionId, includeSensitive: true);
            if (user == null)
            {
                _logger.LogWarning("Login failed: user with email {Email} not found", request.Email);
                return Unauthorized(new { message = "Credenziali non valide" });
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: user {Email} is inactive", request.Email);
                return Unauthorized(new { message = "Utente disabilitato" });
            }

            if (string.IsNullOrWhiteSpace(user.Password) || !VerifyPassword(user.Password, request.Password))
            {
                _logger.LogWarning("Login failed: invalid password for user {Email}", request.Email);
                return Unauthorized(new { message = "Credenziali non valide" });
            }

            user.Password = null;

            var tokenResult = _jwtTokenService.GenerateToken(user, response.SessionId, response.SessionTimeout);

            return Ok(new LoginResponseDto
            {
                Token = tokenResult.Token,
                ExpiresAt = tokenResult.ExpiresAt,
                ExpiresInSeconds = tokenResult.ExpiresInSeconds,
                User = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "Login failed", error = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var sessionId = GetSessionId();
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest(new { message = "Sessione SAP non disponibile" });
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

    [Authorize]
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

    private static bool VerifyPassword(string storedPassword, string providedPassword)
    {
        // TODO: replace with proper hashing when passwords are stored securely
        return string.Equals(storedPassword, providedPassword, StringComparison.Ordinal);
    }
}

