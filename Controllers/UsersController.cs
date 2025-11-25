using ADOTTA.Projects.Suite.Api.DTOs;
using ADOTTA.Projects.Suite.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ADOTTA.Projects.Suite.Api.Extensions;

namespace ADOTTA.Projects.Suite.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id, GetSessionId());
            if (user == null)
            {
                return NotFound(new { message = $"User '{id}' not found" });
            }
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, new { message = "Error retrieving user", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] UserDto user)
    {
        try
        {
            var created = await _userService.CreateUserAsync(user, GetSessionId());
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "Error creating user", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UserDto user)
    {
        try
        {
            var updated = await _userService.UpdateUserAsync(id, user, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new { message = "Error updating user", error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _userService.DeleteUserAsync(id, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, new { message = "Error deleting user", error = ex.Message });
        }
    }
}

