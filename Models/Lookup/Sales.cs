namespace ADOTTA.Projects.Suite.Api.Models.Lookup;

public class Sales
{
    public string? Id { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefono { get; set; }
    public string? Zona { get; set; }
    public string? RegioneDiCompetenza { get; set; }
    public int? ProgettiGestiti { get; set; }
}

