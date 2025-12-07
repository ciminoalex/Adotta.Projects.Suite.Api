using ADOTTA.Projects.Suite.Api.DTOs;

namespace ADOTTA.Projects.Suite.Api.Services;

public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync(string? query, string sessionId);
    Task<UserDto?> GetUserByIdAsync(string code, string sessionId);
    Task<UserDto?> GetUserByEmailAsync(string email, string sessionId, bool includeSensitive = false);
    Task<UserDto> CreateUserAsync(UserDto user, string sessionId);
    Task<UserDto> UpdateUserAsync(string code, UserDto user, string sessionId);
    Task DeleteUserAsync(string code, string sessionId);
}

