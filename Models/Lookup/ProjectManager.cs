namespace ADOTTA.Projects.Suite.Api.Models.Lookup;

public class ProjectManager
{
    public string Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public int? ProgettiAttivi { get; set; }
    public string? Esperienza { get; set; }
    public List<string>? Certificazioni { get; set; }
}

