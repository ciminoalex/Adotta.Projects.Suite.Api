namespace ADOTTA.Projects.Suite.Api.Models;

public class StoricoModifica
{
    public int Id { get; set; }
    public string NumeroProgetto { get; set; } = string.Empty;
    public DateTime DataModifica { get; set; }
    public string UtenteModifica { get; set; } = string.Empty;
    public string CampoModificato { get; set; } = string.Empty;
    public string? ValorePrecedente { get; set; }
    public string? NuovoValore { get; set; }
    public string? VersioneWIC { get; set; }
}

