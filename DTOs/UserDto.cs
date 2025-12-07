using System.Text.Json.Serialization;

namespace ADOTTA.Projects.Suite.Api.DTOs;

public class UserDto
{
    public string Code { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Ruolo { get; set; } = string.Empty;
    public string? TeamTecnico { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Password { get; set; }
}

