using System.Text.Json;
using ADOTTA.Projects.Suite.Api.DTOs;
using ADOTTA.Projects.Suite.Api.Services.Mappers;

namespace ADOTTA.Projects.Suite.Api.Services;

public class ProjectService : IProjectService
{
    private readonly ISAPServiceLayerClient _sapClient;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(ISAPServiceLayerClient sapClient, ILogger<ProjectService> logger)
    {
        _sapClient = sapClient;
        _logger = logger;
    }

    public async Task<List<ProjectDto>> GetAllProjectsAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("AX_ADT_PROJECT", null, sessionId);
        return sapData.Select(MapToProjectDto).ToList();
    }

    public async Task<ProjectDto?> GetProjectByCodeAsync(string numeroProgetto, string sessionId)
    {
        try
        {
            var sapData = await _sapClient.GetRecordAsync<JsonElement>("AX_ADT_PROJECT", numeroProgetto, sessionId);
            if (sapData.ValueKind == JsonValueKind.Undefined || sapData.ValueKind == JsonValueKind.Null) return null;
            
            // Child tables are included in the response, no need for separate calls
            var project = ProjectMapper.MapSapUDOToProject(sapData);
            
            return project;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<ProjectDto> CreateProjectAsync(ProjectDto project, string sessionId)
    {
        var sapUDO = ProjectMapper.MapProjectToSapUDO(project);
        var result = await _sapClient.CreateRecordAsync<JsonElement>("AX_ADT_PROJECT", sapUDO, sessionId);
        return ProjectMapper.MapSapUDOToProject(result);
    }

    public async Task<ProjectDto> UpdateProjectAsync(string numeroProgetto, ProjectDto project, string sessionId)
    {
        var sapUDO = ProjectMapper.MapProjectToSapUDO(project);
        var result = await _sapClient.UpdateRecordAsync<JsonElement>("AX_ADT_PROJECT", numeroProgetto, sapUDO, sessionId);
        return ProjectMapper.MapSapUDOToProject(result);
    }

    public async Task DeleteProjectAsync(string numeroProgetto, string sessionId)
    {
        await _sapClient.DeleteRecordAsync("AX_ADT_PROJECT", numeroProgetto, sessionId);
    }

    public async Task<List<ProjectDto>> SearchProjectsAsync(string searchTerm, string sessionId)
    {
        var filter = $"(contains(Code, '{searchTerm}') or contains(Name, '{searchTerm}') or contains(U_Cliente, '{searchTerm}'))";
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("AX_ADT_PROJECT", filter, sessionId);
        return sapData.Select(MapToProjectDto).ToList();
    }

    public async Task<List<ProjectDto>> FilterProjectsAsync(FilterRequestDto filter, string sessionId)
    {
        var filterConditions = new List<string>();
        
        if (!string.IsNullOrEmpty(filter.Stato))
            filterConditions.Add($"U_StatoProgetto eq '{filter.Stato}'");
        
        if (!string.IsNullOrEmpty(filter.Cliente))
            filterConditions.Add($"contains(U_Cliente, '{filter.Cliente}')");
        
        if (!string.IsNullOrEmpty(filter.ProjectManager))
            filterConditions.Add($"contains(U_ProjectManager, '{filter.ProjectManager}')");
        
        if (filter.DataCreazioneDa.HasValue)
            filterConditions.Add($"U_DataCreazione ge '{filter.DataCreazioneDa.Value:yyyy-MM-dd}'");
        
        if (filter.DataCreazioneA.HasValue)
            filterConditions.Add($"U_DataCreazione le '{filter.DataCreazioneA.Value:yyyy-MM-dd}'");
        
        var filterStr = string.Join(" and ", filterConditions);
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("AX_ADT_PROJECT", filterStr, sessionId);
        return sapData.Select(MapToProjectDto).ToList();
    }

    public async Task<List<LivelloProgettoDto>> GetLivelliAsync(string numeroProgetto, string sessionId)
    {
        // Child tables can only be retrieved through parent project
        var project = await GetProjectByCodeAsync(numeroProgetto, sessionId);
        return project?.Livelli ?? new List<LivelloProgettoDto>();
    }

    public async Task<List<ProdottoProgettoDto>> GetProdottiAsync(string numeroProgetto, string sessionId)
    {
        // Child tables can only be retrieved through parent project
        var project = await GetProjectByCodeAsync(numeroProgetto, sessionId);
        return project?.Prodotti ?? new List<ProdottoProgettoDto>();
    }

    public async Task<List<StoricoModificaDto>> GetStoricoAsync(string numeroProgetto, string sessionId)
    {
        // Child tables can only be retrieved through parent project
        var project = await GetProjectByCodeAsync(numeroProgetto, sessionId);
        if (project == null) return new List<StoricoModificaDto>();
        
        // Extract storico from the project response
        var sapData = await _sapClient.GetRecordAsync<JsonElement>("AX_ADT_PROJECT", numeroProgetto, sessionId);
        if (sapData.ValueKind == JsonValueKind.Undefined || sapData.ValueKind == JsonValueKind.Null)
            return new List<StoricoModificaDto>();
        
        var storico = new List<StoricoModificaDto>();
        if (sapData.TryGetProperty("AX_ADT_PROHISTCollection", out var storicoArray) && storicoArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in storicoArray.EnumerateArray())
            {
                storico.Add(MapToStoricoDto(item));
            }
        }
        
        return storico;
    }

    public async Task<LivelloProgettoDto> CreateLivelloAsync(string numeroProgetto, LivelloProgettoDto livello, string sessionId)
    {
        var sapUDO = ProjectMapper.MapLivelloToSap(livello, numeroProgetto);
        var result = await _sapClient.CreateRecordAsync<JsonElement>("@AX_ADT_PROJLVL", sapUDO, sessionId);
        return MapToLivelloDto(result);
    }

    public async Task DeleteLivelloAsync(string numeroProgetto, int livelloId, string sessionId)
    {
        var code = $"{numeroProgetto}-L{livelloId}";
        await _sapClient.DeleteRecordAsync("@AX_ADT_PROJLVL", code, sessionId);
    }

    public async Task<ProdottoProgettoDto> CreateProdottoAsync(string numeroProgetto, ProdottoProgettoDto prodotto, string sessionId)
    {
        var sapUDO = ProjectMapper.MapProdottoToSap(prodotto, numeroProgetto);
        var result = await _sapClient.CreateRecordAsync<JsonElement>("@AX_ADT_PROPRD", sapUDO, sessionId);
        return MapToProdottoDto(result);
    }

    public async Task DeleteProdottoAsync(string numeroProgetto, int prodottoId, string sessionId)
    {
        var code = $"{numeroProgetto}-P{prodottoId}";
        await _sapClient.DeleteRecordAsync("@AX_ADT_PROPRD", code, sessionId);
    }

    public async Task<List<StoricoModificaDto>> CreateWicSnapshotAsync(string numeroProgetto, string sessionId)
    {
        // Get current project
        var project = await GetProjectByCodeAsync(numeroProgetto, sessionId);
        if (project == null) return new List<StoricoModificaDto>();

        // Create snapshot entries for all project fields
        var snapshot = new List<StoricoModificaDto>
        {
            new StoricoModificaDto
            {
                NumeroProgetto = numeroProgetto,
                DataModifica = DateTime.UtcNow,
                UtenteModifica = "System",
                CampoModificato = "WIC Snapshot",
                Descrizione = $"WIC Snapshot created at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                VersioneWIC = project.VersioneWIC ?? "1.0"
            }
        };

        return snapshot;
    }

    public async Task<ProjectStatsDto> GetProjectStatsAsync(string sessionId)
    {
        var allProjects = await GetAllProjectsAsync(sessionId);
        
        return new ProjectStatsDto
        {
            ProgettiAttivi = allProjects.Count(p => p.StatoProgetto == Models.Enums.ProjectStatus.ON_GOING),
            ValorePortfolio = allProjects.Sum(p => p.ValoreProgetto ?? 0),
            InstallazioniMese = allProjects.Count(p => p.DataInizioInstallazione?.Month == DateTime.Now.Month),
            ProgettiRitardo = allProjects.Count(p => p.IsInRitardo)
        };
    }

    public async Task<List<ProjectStatsByStatusDto>> GetStatsByStatusAsync(string sessionId)
    {
        var allProjects = await GetAllProjectsAsync(sessionId);
        
        return allProjects
            .GroupBy(p => p.StatoProgetto.ToString())
            .Select(g => new ProjectStatsByStatusDto
            {
                Stato = g.Key,
                Count = g.Count()
            })
            .ToList();
    }

    public async Task<List<ProjectStatsByMonthDto>> GetStatsByMonthAsync(string sessionId)
    {
        var allProjects = await GetAllProjectsAsync(sessionId);
        
        return allProjects
            .GroupBy(p => p.DataCreazione.ToString("MMM yyyy"))
            .OrderBy(g => g.First().DataCreazione)
            .Select(g => new ProjectStatsByMonthDto
            {
                Label = g.Key,
                Value = g.Count()
            })
            .ToList();
    }

    private ProjectDto MapToProjectDto(JsonElement sapData)
    {
        return ProjectMapper.MapSapUDOToProject(sapData);
    }

    private LivelloProgettoDto MapToLivelloDto(JsonElement sapData)
    {
        return new LivelloProgettoDto
        {
            Id = sapData.TryGetProperty("Code", out var code) ? int.Parse(code.GetString() ?? "0") : 0,
            NumeroProgetto = sapData.TryGetProperty("U_Parent", out var parent) ? parent.GetString() ?? "" : "",
            Nome = sapData.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : "",
            Ordine = sapData.TryGetProperty("U_Ordine", out var order) ? order.GetInt32() : 0,
            Descrizione = sapData.TryGetProperty("U_Descrizione", out var desc) ? desc.GetString() : null,
            DataInizioInstallazione = sapData.TryGetProperty("U_DataInizio", out var start) && DateTime.TryParse(start.GetString(), out var dtStart) ? dtStart : null,
            DataFineInstallazione = sapData.TryGetProperty("U_DataFine", out var end) && DateTime.TryParse(end.GetString(), out var dtEnd) ? dtEnd : null,
            DataCaricamento = sapData.TryGetProperty("U_DataCaricamento", out var load) && DateTime.TryParse(load.GetString(), out var dtLoad) ? dtLoad : null
        };
    }

    private ProdottoProgettoDto MapToProdottoDto(JsonElement sapData)
    {
        return new ProdottoProgettoDto
        {
            Id = sapData.TryGetProperty("Code", out var code) ? int.Parse(code.GetString() ?? "0") : 0,
            NumeroProgetto = sapData.TryGetProperty("U_Parent", out var parent) ? parent.GetString() ?? "" : "",
            TipoProdotto = sapData.TryGetProperty("U_TipoProdotto", out var tipo) ? tipo.GetString() ?? "" : "",
            Variante = sapData.TryGetProperty("U_Variante", out var variant) ? variant.GetString() ?? "" : "",
            QMq = sapData.TryGetProperty("U_QMq", out var qmq) ? qmq.GetDecimal() : 0,
            QFt = sapData.TryGetProperty("U_QFt", out var qft) ? qft.GetDecimal() : 0
        };
    }

    private StoricoModificaDto MapToStoricoDto(JsonElement sapData)
    {
        return new StoricoModificaDto
        {
            Id = sapData.TryGetProperty("Code", out var code) ? int.Parse(code.GetString() ?? "0") : 0,
            NumeroProgetto = sapData.TryGetProperty("U_Parent", out var parent) ? parent.GetString() ?? "" : "",
            DataModifica = sapData.TryGetProperty("U_DataModifica", out var date) && DateTime.TryParse(date.GetString(), out var dt) ? dt : DateTime.MinValue,
            UtenteModifica = sapData.TryGetProperty("U_UtenteModifica", out var user) ? user.GetString() ?? "" : "",
            CampoModificato = sapData.TryGetProperty("Name", out var field) ? field.GetString() ?? "" : "",
            ValorePrecedente = sapData.TryGetProperty("U_ValorePrecedente", out var oldVal) ? oldVal.GetString() : null,
            NuovoValore = sapData.TryGetProperty("U_NuovoValore", out var newVal) ? newVal.GetString() : null,
            VersioneWIC = sapData.TryGetProperty("U_VersioneWIC", out var version) ? version.GetString() : null,
            Descrizione = null
        };
    }
}

