namespace ADOTTA.Projects.Suite.Api.Models;

public class ProdottoProgetto
{
    public int Id { get; set; }
    public string NumeroProgetto { get; set; } = string.Empty;
    public string TipoProdotto { get; set; } = string.Empty;  // Metafora / Wallen / Armonica
    public string Variante { get; set; } = string.Empty;
    public decimal QMq { get; set; }  // Quantità in metri quadri
    public decimal QFt { get; set; }  // Quantità in piedi quadri
    public string? LivelloId { get; set; }
}

