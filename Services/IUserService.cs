using ADOTTA.Projects.Suite.Api.DTOs;

namespace ADOTTA.Projects.Suite.Api.Services;

public interface IUserService
{
    Task<List<UserDto>> GetUsersAsync(string? query, string sessionId);
    Task<UserDto?> GetUserByIdAsync(int id, string sessionId);
    Task<UserDto?> GetUserByEmailAsync(string email, string sessionId);
    Task<UserDto> CreateUserAsync(UserDto user, string sessionId);
    Task<UserDto> UpdateUserAsync(int id, UserDto user, string sessionId);
    Task DeleteUserAsync(int id, string sessionId);
}

