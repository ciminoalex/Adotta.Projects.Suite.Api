namespace ADOTTA.Projects.Suite.Api.Models.Lookup;

public class Citta
{
    public string Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string StatoId { get; set; } = string.Empty;
    public string? Cap { get; set; }
    public string? Provincia { get; set; }
    public string? Regione { get; set; }
}

