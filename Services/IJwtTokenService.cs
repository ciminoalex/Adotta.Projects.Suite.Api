using ADOTTA.Projects.Suite.Api.DTOs;

namespace ADOTTA.Projects.Suite.Api.Services;

public interface IJwtTokenService
{
    JwtTokenResult GenerateToken(UserDto user, string sapSessionId, int sapSessionTimeoutMinutes);
}

public class JwtTokenResult
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int ExpiresInSeconds { get; set; }
}


