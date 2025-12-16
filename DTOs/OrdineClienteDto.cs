namespace ADOTTA.Projects.Suite.Api.DTOs;

public class OrdineClienteDto
{
    public int DocNum { get; set; }
    public int DocEntry { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public DateTime? DocDate { get; set; }
    public DateTime? DocDueDate { get; set; }
    public DateTime? TaxDate { get; set; }
    public decimal? DocTotal { get; set; }
    public string? DocStatus { get; set; }
    public string? Comments { get; set; }
    public string? City { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? SalesPersonCode { get; set; }
    public string? Currency { get; set; }
    public int? NumAtCard { get; set; }
}