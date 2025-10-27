namespace ADOTTA.Projects.Suite.Api.Models.Lookup;

public class SquadraInstallazione
{
    public string Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public string? Contatto { get; set; }
    public bool Disponibilita { get; set; }
    public List<string>? Competenze { get; set; }
    public int? NumeroMembri { get; set; }
}

