namespace ADOTTA.Projects.Suite.Api.Models.Lookup;

public class TeamTecnico
{
    public string? Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Specializzazione { get; set; }
    public List<string>? Membri { get; set; }
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public bool Disponibilita { get; set; }
}

