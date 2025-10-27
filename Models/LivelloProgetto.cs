namespace ADOTTA.Projects.Suite.Api.Models;

public class LivelloProgetto
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

