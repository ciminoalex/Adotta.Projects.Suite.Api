using ADOTTA.Projects.Suite.Api.Models.Enums;

namespace ADOTTA.Projects.Suite.Api.DTOs;

public class FilterRequestDto
{
    public ProjectStatus? Stato { get; set; }
    public string? Cliente { get; set; }
    public string? ProjectManager { get; set; }
    public string? TeamTecnico { get; set; }
    public DateTime? DataCreazioneDa { get; set; }
    public DateTime? DataCreazioneA { get; set; }
}

public class ProjectStatsDto
{
    public int ProgettiAttivi { get; set; }
    public decimal ValorePortfolio { get; set; }
    public int InstallazioniMese { get; set; }
    public int ProgettiRitardo { get; set; }
}

public class ProjectStatsByStatusDto
{
    public string Stato { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ProjectStatsByMonthDto
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

