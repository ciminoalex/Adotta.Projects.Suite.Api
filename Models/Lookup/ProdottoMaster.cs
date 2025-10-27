namespace ADOTTA.Projects.Suite.Api.Models.Lookup;

public class ProdottoMaster
{
    public string Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;  // Metafora / Wallen / Armonica
    public string UnitaMisura { get; set; } = string.Empty;
    public string? CodiceSAP { get; set; }
    public string? Descrizione { get; set; }
    public List<string>? VariantiDisponibili { get; set; }
}

