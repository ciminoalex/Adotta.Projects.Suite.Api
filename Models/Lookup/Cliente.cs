namespace ADOTTA.Projects.Suite.Api.Models.Lookup;

public class Cliente
{
    public string Id { get; set; } = string.Empty;
    public string CardCode { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? PartitaIVA { get; set; }
    public string? Contatto { get; set; }
    public string? IndirizzoCompleto { get; set; }
    public string? Citta { get; set; }
    public string? Provincia { get; set; }
    public string? Cap { get; set; }
    public string? Stato { get; set; }
    public string? Note { get; set; }
    public string ValidFor { get; set; } = "Y";
    public List<BPAddress>? Addresses { get; set; }
}

public class BPAddress
{
    public string AddressName { get; set; } = string.Empty;
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? ZipCode { get; set; }
}

