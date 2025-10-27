namespace ADOTTA.Projects.Suite.Api.Models.Lookup;

public class TeamAPL
{
    public string Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Area { get; set; }
    public List<string>? Competenze { get; set; }
}

