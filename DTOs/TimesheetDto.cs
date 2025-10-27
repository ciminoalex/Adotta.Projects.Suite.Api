namespace ADOTTA.Projects.Suite.Api.DTOs;

public class TimesheetEntryDto
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

public class TimesheetOverviewDto
{
    public List<TimesheetProjectDto> Timesheets { get; set; } = new();
    public TimesheetSummaryDto Summary { get; set; } = new();
}

public class TimesheetProjectDto
{
    public string NumeroProgetto { get; set; } = string.Empty;
    public string NomeProgetto { get; set; } = string.Empty;
    public string Cliente { get; set; } = string.Empty;
    public double TotaleOre { get; set; }
    public int NumeroRendicontazioni { get; set; }
    public DateTime? UltimaRendicontazione { get; set; }
    public List<TimesheetEntryDto> Rendicontazioni { get; set; } = new();
}

public class TimesheetSummaryDto
{
    public double TotaleOre { get; set; }
    public int TotaleRendicontazioni { get; set; }
    public int ProgettiRendicontati { get; set; }
    public double MediaOrePerProgetto { get; set; }
}

