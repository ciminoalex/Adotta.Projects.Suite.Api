namespace ADOTTA.Projects.Suite.Api.DTOs;

public class LoginRequestDto
{
    public string CompanyDB { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string SessionId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int SessionTimeout { get; set; }
}

