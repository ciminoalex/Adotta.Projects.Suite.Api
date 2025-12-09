using ADOTTA.Projects.Suite.Api.DTOs;
using ADOTTA.Projects.Suite.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ADOTTA.Projects.Suite.Api.Extensions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace ADOTTA.Projects.Suite.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly IServiceProvider _serviceProvider;

    public UsersController(IUserService userService, ILogger<UsersController> logger, IServiceProvider serviceProvider)
    {
        _userService = userService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    private string GetSessionId() => HttpContext.GetSapSessionId();

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll([FromQuery] string? q)
    {
        try
        {
            var users = await _userService.GetUsersAsync(q, GetSessionId());
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, new { message = "Error retrieving users", error = ex.Message });
        }
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<UserDto>> GetById(string code)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(code, GetSessionId());
            if (user == null)
            {
                return NotFound(new { message = $"User '{code}' not found" });
            }
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserCode}", code);
            return StatusCode(500, new { message = "Error retrieving user", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] UserDto user)
    {
        try
        {
            // Validate the user DTO
            var validator = _serviceProvider.GetRequiredService<IValidator<UserDto>>();
            var validationResult = await validator.ValidateAsync(user);
            if (!validationResult.IsValid)
            {
                // Convert PascalCase property names to camelCase for the error response
                var errors = validationResult.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(g.Key) ?? g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { 
                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    errors = errors
                });
            }

            var created = await _userService.CreateUserAsync(user, GetSessionId());
            return CreatedAtAction(nameof(GetById), new { code = created.Code }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "Error creating user", error = ex.Message });
        }
    }

    [HttpPut("{code}")]
    public async Task<ActionResult<UserDto>> Update(string code, [FromBody] UserDto user)
    {
        try
        {
            // Validate the user DTO
            var validator = _serviceProvider.GetRequiredService<IValidator<UserDto>>();
            var validationResult = await validator.ValidateAsync(user);
            if (!validationResult.IsValid)
            {
                // Convert PascalCase property names to camelCase for the error response
                var errors = validationResult.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(g.Key) ?? g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { 
                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    errors = errors
                });
            }

            var updated = await _userService.UpdateUserAsync(code, user, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserCode}", code);
            return StatusCode(500, new { message = "Error updating user", error = ex.Message });
        }
    }

    [HttpDelete("{code}")]
    public async Task<IActionResult> Delete(string code)
    {
        try
        {
            await _userService.DeleteUserAsync(code, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserCode}", code);
            return StatusCode(500, new { message = "Error deleting user", error = ex.Message });
        }
    }
}

