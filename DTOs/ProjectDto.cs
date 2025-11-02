namespace ADOTTA.Projects.Suite.Api.DTOs;

public class ProjectDto
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
    public List<LivelloProgettoDto>? Livelli { get; set; }
    public List<ProdottoProgettoDto>? Prodotti { get; set; }
}

public class LivelloProgettoDto
{
    public int Id { get; set; }
    public string NumeroProgetto { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public int Ordine { get; set; }
    public string? Descrizione { get; set; }
    public DateTime? DataInizioInstallazione { get; set; }
    public DateTime? DataFineInstallazione { get; set; }
    public DateTime? DataCaricamento { get; set; }
}

public class ProdottoProgettoDto
{
    public int Id { get; set; }
    public string NumeroProgetto { get; set; } = string.Empty;
    public string TipoProdotto { get; set; } = string.Empty;
    public string Variante { get; set; } = string.Empty;
    public decimal QMq { get; set; }
    public decimal QFt { get; set; }
}

public class StoricoModificaDto
{
    public int Id { get; set; }
    public string NumeroProgetto { get; set; } = string.Empty;
    public DateTime DataModifica { get; set; }
    public string UtenteModifica { get; set; } = string.Empty;
    public string CampoModificato { get; set; } = string.Empty;
    public string? ValorePrecedente { get; set; }
    public string? NuovoValore { get; set; }
    public string? VersioneWIC { get; set; }
    public string? Descrizione { get; set; }
}

