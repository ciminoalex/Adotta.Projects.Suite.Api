namespace ADOTTA.Projects.Suite.Api.Models;

public class TimesheetEntry
{
    public int Id { get; set; }
    public string ProgettoId { get; set; } = string.Empty;
    public string NumeroProgetto { get; set; } = string.Empty;
    public string NomeProgetto { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public DateTime DataRendicontazione { get; set; }
    public double OreLavorate { get; set; }
    public string Note { get; set; } = string.Empty;
    public string Utente { get; set; } = string.Empty;
    public DateTime? DataCreazione { get; set; }
    public DateTime? UltimaModifica { get; set; }
}

