
namespace ADOTTA.Projects.Suite.Api.Models;

public class Project
{
    public string NumeroProgetto { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public string NomeProgetto { get; set; } = string.Empty;
    public string Citta { get; set; } = string.Empty;
    public string Stato { get; set; } = string.Empty;
    public string? TeamTecnico { get; set; }
    public string? TeamAPL { get; set; }
    public string? Sales { get; set; }
    public string? ProjectManager { get; set; }
    public string? TeamInstallazione { get; set; }
    public DateTime DataCreazione { get; set; }
    public DateTime? DataInizioInstallazione { get; set; }
    public DateTime? DataFineInstallazione { get; set; }
    public string? VersioneWIC { get; set; }
    public DateTime? UltimaModifica { get; set; }
    public string? StatoProgetto { get; set; }
    public bool IsInRitardo { get; set; }
    public string? Note { get; set; }
    public decimal? ValoreProgetto { get; set; }
    public decimal? MarginePrevisto { get; set; }
    public decimal? CostiSostenuti { get; set; }
    public string? CodiceSAP { get; set; }
    
    // Navigation properties
    public List<LivelloProgetto> Livelli { get; set; } = new();
    public List<ProdottoProgetto> Prodotti { get; set; } = new();
    public List<StoricoModifica> Storico { get; set; } = new();
}

