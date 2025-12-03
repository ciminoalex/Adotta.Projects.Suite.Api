using System.Linq;
using System.Text;
using System.Text.Json;
using ADOTTA.Projects.Suite.Api.DTOs;
using ADOTTA.Projects.Suite.Api.Services.Mappers;

namespace ADOTTA.Projects.Suite.Api.Services;

public class ProjectService : IProjectService
{
    private const string ProjectTable = "AX_ADT_PROJECT";
    private const string LivelliTable = "@AX_ADT_PROJLVL";
    private const string ProdottiTable = "@AX_ADT_PROPRD";
    private const string MessaggiTable = "AX_ADT_PROMSG";
    private const string ChangeLogTable = "@AX_ADT_PROCHG";

    private static readonly IReadOnlyDictionary<string, string> ProjectPatchMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["cliente"] = "U_Cliente",
        ["nomeProgetto"] = "Name",
        ["citta"] = "U_Citta",
        ["stato"] = "U_Stato",
        ["teamTecnico"] = "U_TeamTecnico",
        ["teamApl"] = "U_TeamAPL",
        ["sales"] = "U_Sales",
        ["projectManager"] = "U_ProjectManager",
        ["teamInstallazione"] = "U_TeamInstallazione",
        ["dataCreazione"] = "U_DataCreazione",
        ["dataInizioInstallazione"] = "U_DataInizioInstall",
        ["dataFineInstallazione"] = "U_DataFineInstall",
        ["versioneWic"] = "U_VersioneWIC",
        ["ultimaModifica"] = "U_UltimaModifica",
        ["statoProgetto"] = "U_StatoProgetto",
        ["isInRitardo"] = "U_IsInRitardo",
        ["note"] = "U_Note",
        ["valoreProgetto"] = "U_ValoreProgetto",
        ["marginePrevisto"] = "U_MarginePrevisto",
        ["costiSostenuti"] = "U_CostiSostenuti",
        ["quantitaTotaleMq"] = "U_QtaTotaleMq",
        ["quantitaTotaleFt"] = "U_QtaTotaleFt"
    };

    private readonly ISAPServiceLayerClient _sapClient;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(ISAPServiceLayerClient sapClient, ILogger<ProjectService> logger)
    {
        _sapClient = sapClient;
        _logger = logger;
    }

    public async Task<List<ProjectDto>> GetAllProjectsAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(ProjectTable, null, sessionId);
        return sapData.Select(MapToProjectDto).ToList();
    }

    public async Task<ProjectDto?> GetProjectByCodeAsync(string numeroProgetto, string sessionId)
    {
        try
        {
            var sapData = await _sapClient.GetRecordAsync<JsonElement>(ProjectTable, numeroProgetto, sessionId);
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
        var result = await _sapClient.CreateRecordAsync<JsonElement>(ProjectTable, sapUDO, sessionId);
        return ProjectMapper.MapSapUDOToProject(result);
    }

    public async Task<ProjectDto> UpdateProjectAsync(string numeroProgetto, ProjectDto project, string sessionId, string? utente = null)
    {
        // Recupera la versione corrente prima dell'update
        var existingProject = await GetProjectByCodeAsync(numeroProgetto, sessionId);
        if (existingProject == null)
        {
            throw new InvalidOperationException($"Project {numeroProgetto} not found");
        }

        // Confronta le versioni e genera le modifiche PRIMA dell'update
        var changeLogEntries = GenerateChangeLogEntries(numeroProgetto, existingProject, project, utente);

        // Include le modifiche nel payload SAP come collection
        var sapUDO = ProjectMapper.MapProjectToSapUDO(project, changeLogEntries);
        var result = await _sapClient.UpdateRecordAsync<JsonElement>(ProjectTable, numeroProgetto, sapUDO, sessionId);
        
        // SAP returns 204 No Content on PATCH, so re-fetch the updated project
        ProjectDto updated;
        if (result.ValueKind == JsonValueKind.Undefined || result.ValueKind == JsonValueKind.Null)
        {
            updated = await GetProjectByCodeAsync(numeroProgetto, sessionId) 
                ?? throw new InvalidOperationException($"Project {numeroProgetto} not found after update");
        }
        else
        {
            updated = ProjectMapper.MapSapUDOToProject(result);
        }

        return updated;
    }

    public async Task<ProjectDto> PatchProjectAsync(string numeroProgetto, JsonElement patchDocument, string sessionId, string? utente = null)
    {
        // Get existing project to merge with patch data
        var existingProject = await GetProjectByCodeAsync(numeroProgetto, sessionId);
        if (existingProject == null)
        {
            throw new InvalidOperationException($"Project {numeroProgetto} not found");
        }

        // Salva una copia della versione originale per il confronto
        var originalProject = JsonSerializer.Deserialize<ProjectDto>(
            JsonSerializer.Serialize(existingProject),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        ) ?? throw new InvalidOperationException("Failed to clone project");

        // Deserialize patch document to ProjectDto to merge collections properly
        var patchDto = JsonSerializer.Deserialize<ProjectDto>(patchDocument.GetRawText(), new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        if (patchDto == null)
        {
            throw new ArgumentException("Invalid patch document");
        }

        // Merge patch data with existing project
        // Update project fields from patch
        if (patchDocument.TryGetProperty("cliente", out _)) existingProject.Cliente = patchDto.Cliente;
        if (patchDocument.TryGetProperty("nomeProgetto", out _)) existingProject.NomeProgetto = patchDto.NomeProgetto;
        if (patchDocument.TryGetProperty("citta", out _)) existingProject.Citta = patchDto.Citta;
        if (patchDocument.TryGetProperty("stato", out _)) existingProject.Stato = patchDto.Stato;
        if (patchDocument.TryGetProperty("dataCreazione", out _)) existingProject.DataCreazione = patchDto.DataCreazione;
        if (patchDocument.TryGetProperty("dataInizioInstallazione", out _)) existingProject.DataInizioInstallazione = patchDto.DataInizioInstallazione;
        if (patchDocument.TryGetProperty("dataFineInstallazione", out _)) existingProject.DataFineInstallazione = patchDto.DataFineInstallazione;
        if (patchDocument.TryGetProperty("ultimaModifica", out _)) existingProject.UltimaModifica = patchDto.UltimaModifica;
        if (patchDocument.TryGetProperty("valoreProgetto", out _)) existingProject.ValoreProgetto = patchDto.ValoreProgetto;
        if (patchDocument.TryGetProperty("marginePrevisto", out _)) existingProject.MarginePrevisto = patchDto.MarginePrevisto;
        if (patchDocument.TryGetProperty("costiSostenuti", out _)) existingProject.CostiSostenuti = patchDto.CostiSostenuti;
        if (patchDocument.TryGetProperty("statoProgetto", out _)) existingProject.StatoProgetto = patchDto.StatoProgetto;
        if (patchDocument.TryGetProperty("note", out _)) existingProject.Note = patchDto.Note;
        if (patchDocument.TryGetProperty("teamTecnico", out _)) existingProject.TeamTecnico = patchDto.TeamTecnico;
        if (patchDocument.TryGetProperty("teamAPL", out _)) existingProject.TeamAPL = patchDto.TeamAPL;
        if (patchDocument.TryGetProperty("sales", out _)) existingProject.Sales = patchDto.Sales;
        if (patchDocument.TryGetProperty("projectManager", out _)) existingProject.ProjectManager = patchDto.ProjectManager;
        if (patchDocument.TryGetProperty("teamInstallazione", out _)) existingProject.TeamInstallazione = patchDto.TeamInstallazione;
        if (patchDocument.TryGetProperty("versioneWIC", out _)) existingProject.VersioneWIC = patchDto.VersioneWIC;
        if (patchDocument.TryGetProperty("quantitaTotaleMq", out _)) existingProject.QuantitaTotaleMq = patchDto.QuantitaTotaleMq;
        if (patchDocument.TryGetProperty("quantitaTotaleFt", out _)) existingProject.QuantitaTotaleFt = patchDto.QuantitaTotaleFt;

        // Replace collections if present in patch (WebApp always sends complete collections)
        if (patchDocument.TryGetProperty("livelli", out var livelliElement) && livelliElement.ValueKind == JsonValueKind.Array)
        {
            existingProject.Livelli = patchDto.Livelli;
            _logger.LogDebug("Replacing {Count} livelli in PATCH for project {NumeroProgetto}", 
                existingProject.Livelli?.Count ?? 0, numeroProgetto);
        }
        
        if (patchDocument.TryGetProperty("prodotti", out var prodottiElement) && prodottiElement.ValueKind == JsonValueKind.Array)
        {
            existingProject.Prodotti = patchDto.Prodotti;
            _logger.LogDebug("Replacing {Count} prodotti in PATCH for project {NumeroProgetto}", 
                existingProject.Prodotti?.Count ?? 0, numeroProgetto);
        }

        // Confronta le versioni e genera le modifiche PRIMA dell'update
        var changeLogEntries = GenerateChangeLogEntries(numeroProgetto, originalProject, existingProject, utente);

        // Use MapProjectToSapUDO to ensure collections are formatted correctly (same as PUT)
        // This approach worked before - collections were saved correctly
        // Include le modifiche nel payload SAP come collection
        var sapUDO = ProjectMapper.MapProjectToSapUDO(existingProject, changeLogEntries);
        
        _logger.LogDebug("PATCH payload for project {NumeroProgetto} contains {Count} fields: {Fields}", 
            numeroProgetto, ((Dictionary<string, object?>)sapUDO).Count, 
            string.Join(", ", ((Dictionary<string, object?>)sapUDO).Keys));
        
        var result = await _sapClient.UpdateRecordAsync<JsonElement>(ProjectTable, numeroProgetto, sapUDO, sessionId);
        
        // SAP returns 204 No Content on PATCH, so re-fetch the updated project
        ProjectDto updated;
        if (result.ValueKind == JsonValueKind.Undefined || result.ValueKind == JsonValueKind.Null)
        {
            updated = await GetProjectByCodeAsync(numeroProgetto, sessionId) 
                ?? throw new InvalidOperationException($"Project {numeroProgetto} not found after patch");
        }
        else
        {
            updated = ProjectMapper.MapSapUDOToProject(result);
        }

        return updated;
    }

    public async Task DeleteProjectAsync(string numeroProgetto, string sessionId)
    {
        await _sapClient.DeleteRecordAsync(ProjectTable, numeroProgetto, sessionId);
    }

    public async Task<List<ProjectDto>> SearchProjectsAsync(string searchTerm, string sessionId)
    {
        var filter = $"(contains(Code, '{searchTerm}') or contains(Name, '{searchTerm}') or contains(U_Cliente, '{searchTerm}'))";
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(ProjectTable, filter, sessionId);
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
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(ProjectTable, filterStr, sessionId);
        return sapData.Select(MapToProjectDto).ToList();
    }

    public async Task<List<LivelloProgettoDto>> GetLivelliAsync(string numeroProgetto, string sessionId)
    {
        // Child tables can only be retrieved through parent project
        var project = await GetProjectByCodeAsync(numeroProgetto, sessionId);
        if (project == null)
        {
            return new List<LivelloProgettoDto>();
        }

        // Attach prodotti to each livello based on logical U_LivelloId mapping
        var livelli = project.Livelli ?? new List<LivelloProgettoDto>();
        var prodotti = project.Prodotti ?? new List<ProdottoProgettoDto>();

        foreach (var livello in livelli)
        {
            livello.Prodotti = prodotti
                .Where(p => p.LivelloId.HasValue && p.LivelloId.Value == livello.Id)
                .ToList();
        }

        return livelli;
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
        var sapData = await _sapClient.GetRecordAsync<JsonElement>(ProjectTable, numeroProgetto, sessionId);
        if (sapData.ValueKind == JsonValueKind.Undefined || sapData.ValueKind == JsonValueKind.Null)
            return new List<StoricoModificaDto>();
        
        var storico = new List<StoricoModificaDto>();
        
        // Leggi AX_ADT_PROHISTCollection (storico modifiche tradizionale)
        if (sapData.TryGetProperty("AX_ADT_PROHISTCollection", out var storicoArray) && storicoArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in storicoArray.EnumerateArray())
            {
                storico.Add(ProjectMapper.MapStoricoFromSap(item));
            }
        }
        
        // NOTA: I ChangeLog vengono restituiti direttamente tramite GetChangeLogAsync o nel progetto.ChangeLog
        // Non li includiamo qui per evitare duplicati - vengono gestiti separatamente nell'endpoint
        
        return storico;
    }

    public async Task<LivelloProgettoDto> CreateLivelloAsync(string numeroProgetto, LivelloProgettoDto livello, string sessionId)
    {
        var sapUDO = ProjectMapper.MapLivelloToSap(livello, numeroProgetto);
        var result = await _sapClient.CreateRecordAsync<JsonElement>(LivelliTable, sapUDO, sessionId);
        return ProjectMapper.MapLivelloFromSap(result, numeroProgetto);
    }

    public async Task<LivelloProgettoDto> UpdateLivelloAsync(string numeroProgetto, int livelloId, LivelloProgettoDto livello, string sessionId)
    {
        livello.Id = livelloId;
        var sapUDO = ProjectMapper.MapLivelloToSap(livello, numeroProgetto);
        var result = await _sapClient.UpdateRecordAsync<JsonElement>(LivelliTable, $"{numeroProgetto}-L{livelloId}", sapUDO, sessionId);
        return ProjectMapper.MapLivelloFromSap(result, numeroProgetto);
    }

    public async Task DeleteLivelloAsync(string numeroProgetto, int livelloId, string sessionId)
    {
        var code = $"{numeroProgetto}-L{livelloId}";
        await _sapClient.DeleteRecordAsync(LivelliTable, code, sessionId);
    }

    public async Task<ProdottoProgettoDto> CreateProdottoAsync(string numeroProgetto, ProdottoProgettoDto prodotto, string sessionId)
    {
        var sapUDO = ProjectMapper.MapProdottoToSap(prodotto, numeroProgetto);
        var result = await _sapClient.CreateRecordAsync<JsonElement>(ProdottiTable, sapUDO, sessionId);
        return ProjectMapper.MapProdottoFromSap(result, numeroProgetto);
    }

    public async Task<ProdottoProgettoDto> UpdateProdottoAsync(string numeroProgetto, int prodottoId, ProdottoProgettoDto prodotto, string sessionId)
    {
        prodotto.Id = prodottoId;
        var sapUDO = ProjectMapper.MapProdottoToSap(prodotto, numeroProgetto);
        var result = await _sapClient.UpdateRecordAsync<JsonElement>(ProdottiTable, $"{numeroProgetto}-P{prodottoId}", sapUDO, sessionId);
        return ProjectMapper.MapProdottoFromSap(result, numeroProgetto);
    }

    public async Task DeleteProdottoAsync(string numeroProgetto, int prodottoId, string sessionId)
    {
        var code = $"{numeroProgetto}-P{prodottoId}";
        await _sapClient.DeleteRecordAsync(ProdottiTable, code, sessionId);
    }

    public async Task<List<StoricoModificaDto>> CreateWicSnapshotAsync(string numeroProgetto, string sessionId)
    {
        // Get current project
        var project = await GetProjectByCodeAsync(numeroProgetto, sessionId);
        if (project == null) return new List<StoricoModificaDto>();

        var snapshotEntry = new
        {
            Code = Guid.NewGuid().ToString("N"),
            Name = $"WIC Snapshot {DateTime.UtcNow:yyyyMMddHHmmss}",
            U_Parent = numeroProgetto,
            U_DataModifica = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
            U_UtenteModifica = "System",
            U_CampoModificato = "WIC Snapshot",
            U_ValorePrecedente = project.VersioneWIC,
            U_NuovoValore = project.VersioneWIC,
            U_VersioneWIC = project.VersioneWIC ?? "WIC-1.0",
            U_Descrizione = $"Snapshot generata il {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}"
        };

        await _sapClient.CreateRecordAsync<JsonElement>("@AX_ADT_PROHIST", snapshotEntry, sessionId);
        return await GetStoricoAsync(numeroProgetto, sessionId);
    }

    public async Task<List<ProdottoProgettoDto>> GetProdottiByLivelloAsync(string numeroProgetto, int livelloId, string sessionId)
    {
        var filter = $"U_Parent eq '{numeroProgetto}' and U_LivelloId eq '{livelloId}'";
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(ProdottiTable, filter, sessionId);
        return sapData.Select(item => ProjectMapper.MapProdottoFromSap(item, numeroProgetto)).ToList();
    }

    public async Task<List<MessaggioProgettoDto>> GetMessaggiAsync(string numeroProgetto, string sessionId)
    {
        var filter = $"U_Project eq '{numeroProgetto}'";
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(MessaggiTable, filter, sessionId);
        return sapData.Select(item => ProjectMapper.MapMessaggioFromSap(item, numeroProgetto)).ToList();
    }

    public async Task<MessaggioProgettoDto> CreateMessaggioAsync(string numeroProgetto, MessaggioProgettoDto messaggio, string sessionId)
    {
        var payload = MapMessaggioToSap(numeroProgetto, messaggio);
        var created = await _sapClient.CreateRecordAsync<JsonElement>(MessaggiTable, payload, sessionId);
        return ProjectMapper.MapMessaggioFromSap(created, numeroProgetto);
    }

    public async Task<MessaggioProgettoDto> UpdateMessaggioAsync(string numeroProgetto, string messaggioId, MessaggioProgettoDto messaggio, string sessionId)
    {
        messaggio.Id = messaggioId;
        var payload = MapMessaggioToSap(numeroProgetto, messaggio, isUpdate: true, existingCode: messaggioId);
        // Use messaggioId directly as Code if it looks like a GUID, otherwise use BuildMessageCode for backward compatibility
        var code = IsGuid(messaggioId) ? messaggioId : BuildMessageCode(numeroProgetto, messaggioId);
        var updated = await _sapClient.UpdateRecordAsync<JsonElement>(MessaggiTable, code, payload, sessionId);
        return ProjectMapper.MapMessaggioFromSap(updated, numeroProgetto);
    }

    public async Task DeleteMessaggioAsync(string numeroProgetto, string messaggioId, string sessionId)
    {
        // Use messaggioId directly as Code if it looks like a GUID, otherwise use BuildMessageCode for backward compatibility
        var code = IsGuid(messaggioId) ? messaggioId : BuildMessageCode(numeroProgetto, messaggioId);
        await _sapClient.DeleteRecordAsync(MessaggiTable, code, sessionId);
    }

    public async Task<List<ChangeLogDto>> GetChangeLogAsync(string numeroProgetto, string sessionId)
    {
        var filter = $"U_Project eq '{numeroProgetto}'";
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(ChangeLogTable, filter, sessionId);
        return sapData.Select(item => ProjectMapper.MapChangeLogFromSap(item, numeroProgetto)).ToList();
    }

    public async Task<ChangeLogDto> CreateChangeLogAsync(string numeroProgetto, ChangeLogDto change, string sessionId)
    {
        var payload = MapChangeLogToSap(numeroProgetto, change);
        var created = await _sapClient.CreateRecordAsync<JsonElement>(ChangeLogTable, payload, sessionId);
        return ProjectMapper.MapChangeLogFromSap(created, numeroProgetto);
    }

    public async Task<ProjectExportResultDto> ExportProjectsAsync(string format, ProjectExportRequestDto request, string sessionId)
    {
        var projects = await GetAllProjectsAsync(sessionId);
        projects = ApplyExportFilters(projects, request);

        var normalizedFormat = (format ?? "csv").ToLowerInvariant();
        var extension = normalizedFormat switch
        {
            "xlsx" => "xlsx",
            "pdf" => "pdf",
            _ => "csv"
        };

        var contentType = normalizedFormat switch
        {
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "pdf" => "application/pdf",
            _ => "text/csv"
        };

        var content = normalizedFormat switch
        {
            "pdf" => GenerateCsv(projects), // placeholder same data
            "xlsx" => GenerateCsv(projects),
            _ => GenerateCsv(projects)
        };

        return new ProjectExportResultDto
        {
            Content = content,
            ContentType = contentType,
            FileName = $"projects_{DateTime.UtcNow:yyyyMMddHHmmss}.{extension}"
        };
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

    private Dictionary<string, object?> BuildProjectPatchPayload(JsonElement patchDocument)
    {
        var payload = new Dictionary<string, object?>();
        foreach (var property in patchDocument.EnumerateObject())
        {
            // Skip livelli and prodotti as they are handled separately
            if (property.Name.Equals("livelli", StringComparison.OrdinalIgnoreCase) || 
                property.Name.Equals("prodotti", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            
            if (!ProjectPatchMap.TryGetValue(property.Name, out var sapField))
            {
                continue;
            }

            payload[sapField] = ConvertPatchValue(property.Name, property.Value);
        }

        return payload;
    }

    private object BuildLivelliCollection(JsonElement livelliArray, string numeroProgetto)
    {
        var livelliList = new List<(int id, string nome, string descrizione, int ordine, string dataInizio, string dataFine, string dataCaricamento)>();
        var idx = 0;
        
        foreach (var livello in livelliArray.EnumerateArray())
        {
            var id = 0;
            if (livello.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.Number)
            {
                id = idElement.GetInt32();
            }
            
            // If no ID provided, use index-based ID (for new items)
            if (id == 0)
            {
                id = idx + 1;
            }
            
            var nome = livello.TryGetProperty("nome", out var uNome) ? uNome.GetString() ?? "" : "";
            var descrizione = livello.TryGetProperty("descrizione", out var desc) ? desc.GetString() ?? "" : "";
            var ordine = livello.TryGetProperty("ordine", out var ord) && ord.ValueKind == JsonValueKind.Number 
                ? ord.GetInt32() 
                : idx + 1;
            
            var dataInizio = "";
            if (livello.TryGetProperty("dataInizioInstallazione", out var dataInizioProp) && dataInizioProp.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(dataInizioProp.GetString(), out var dtInizio))
                {
                    dataInizio = dtInizio.ToString("yyyy-MM-ddTHH:mm:ss");
                }
            }
            
            var dataFine = "";
            if (livello.TryGetProperty("dataFineInstallazione", out var dataFineProp) && dataFineProp.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(dataFineProp.GetString(), out var dtFine))
                {
                    dataFine = dtFine.ToString("yyyy-MM-ddTHH:mm:ss");
                }
            }
            
            var dataCaricamento = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
            if (livello.TryGetProperty("dataCaricamento", out var dataCaricamentoProp) && dataCaricamentoProp.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(dataCaricamentoProp.GetString(), out var dtCaricamento))
                {
                    dataCaricamento = dtCaricamento.ToString("yyyy-MM-ddTHH:mm:ss");
                }
            }
            
            livelliList.Add((id, nome, descrizione, ordine, dataInizio, dataFine, dataCaricamento));
            idx++;
        }
        
        // Use the same approach as MapProjectToSapUDO: Select().ToList() on anonymous objects
        return livelliList.Select((l, i) => new
        {
            Code = $"{numeroProgetto}-L{l.id}",
            U_Parent = numeroProgetto,
            U_Ordine = l.ordine,
            U_Nome = l.nome,
            U_Descrizione = l.descrizione,
            U_DataInizio = l.dataInizio,
            U_DataFine = l.dataFine,
            U_DataCaricamento = l.dataCaricamento
        }).ToList();
    }

    private object BuildProdottiCollection(JsonElement prodottiArray, string numeroProgetto)
    {
        var prodottiList = new List<(int id, string tipoProdotto, string variante, decimal qmq, decimal qft, string livelloId)>();
        var idx = 0;
        
        foreach (var prodotto in prodottiArray.EnumerateArray())
        {
            var id = 0;
            if (prodotto.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.Number)
            {
                id = idElement.GetInt32();
            }
            
            // If no ID provided, use index-based ID (for new items)
            if (id == 0)
            {
                id = idx + 1;
            }
            
            var tipoProdotto = prodotto.TryGetProperty("tipoProdotto", out var tipo) ? tipo.GetString() ?? "" : "";
            var variante = prodotto.TryGetProperty("variante", out var var) ? var.GetString() ?? "" : "";
            
            var qmq = 0m;
            if (prodotto.TryGetProperty("qMq", out var qmqProp) && qmqProp.ValueKind == JsonValueKind.Number)
            {
                qmq = qmqProp.GetDecimal();
            }
            
            var qft = 0m;
            if (prodotto.TryGetProperty("qFt", out var qftProp) && qftProp.ValueKind == JsonValueKind.Number)
            {
                qft = qftProp.GetDecimal();
            }
            
            var livelloId = "";
            if (prodotto.TryGetProperty("livelloId", out var lvlIdProp) && lvlIdProp.ValueKind == JsonValueKind.Number)
            {
                livelloId = lvlIdProp.GetInt32().ToString();
            }
            
            prodottiList.Add((id, tipoProdotto, variante, qmq, qft, livelloId));
            idx++;
        }
        
        // Use the same approach as MapProjectToSapUDO: Select().ToList() on anonymous objects
        return prodottiList.Select(p => new
        {
            Code = $"{numeroProgetto}-P{p.id}",
            U_Parent = numeroProgetto,
            U_TipoProdotto = p.tipoProdotto,
            U_Variante = p.variante,
            U_QMq = p.qmq,
            U_QFt = p.qft,
            U_LivelloId = p.livelloId
        }).ToList();
    }

    private object? ConvertPatchValue(string propertyName, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                if (DateTime.TryParse(value.GetString(), out var dt))
                {
                    return dt.ToString("yyyy-MM-ddTHH:mm:ss");
                }
                return value.GetString();
            case JsonValueKind.Number:
                if (value.TryGetInt32(out var intVal))
                {
                    return intVal;
                }
                if (value.TryGetDecimal(out var decVal))
                {
                    return decVal;
                }
                return value.GetDouble();
            case JsonValueKind.True:
            case JsonValueKind.False:
                var boolVal = value.GetBoolean();
                return propertyName.Equals("isInRitardo", StringComparison.OrdinalIgnoreCase)
                    ? (boolVal ? "Y" : "N")
                    : boolVal;
            default:
                return value.GetRawText();
        }
    }

    private object MapMessaggioToSap(string numeroProgetto, MessaggioProgettoDto messaggio, bool isUpdate = false, string? existingCode = null)
    {
        string code;
        string name;

        if (isUpdate && !string.IsNullOrEmpty(existingCode))
        {
            // For updates, use the existing Code
            code = existingCode;
            name = existingCode;
        }
        else
        {
            // For new messages, generate a unique GUID for Code and Name to avoid conflicts
            var uniqueGuid = Guid.NewGuid().ToString("N");
            code = uniqueGuid;
            name = uniqueGuid;
        }

        return new
        {
            Code = code,
            Name = name,
            U_Project = numeroProgetto,
            U_Data = messaggio.Data == default ? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") : messaggio.Data.ToString("yyyy-MM-ddTHH:mm:ss"),
            U_Utente = messaggio.Utente,
            U_Messaggio = messaggio.Messaggio,
            U_Tipo = messaggio.Tipo,
            U_Allegato = messaggio.Allegato
        };
    }

    private static bool IsGuid(string value)
    {
        return Guid.TryParse(value, out _) || (value.Length == 32 && value.All(c => char.IsLetterOrDigit(c)));
    }

    private static string BuildMessageCode(string numeroProgetto, string messaggioId) => $"{numeroProgetto}-MSG{messaggioId}";

    private object MapChangeLogToSap(string numeroProgetto, ChangeLogDto change)
    {
        var ensuredId = EnsureEntityId(change.Id);
        change.Id = ensuredId;

        return new
        {
            Code = BuildChangeLogCode(numeroProgetto, ensuredId),
            Name = $"{numeroProgetto}-CHG{ensuredId}",
            U_Project = numeroProgetto,
            U_Data = change.Data == default ? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") : change.Data.ToString("yyyy-MM-ddTHH:mm:ss"),
            U_Utente = change.Utente,
            U_Azione = change.Azione,
            U_Descrizione = change.Descrizione,
            U_DettagliJson = JsonSerializer.Serialize(change.Dettagli ?? new Dictionary<string, string>())
        };
    }

    private static string BuildChangeLogCode(string numeroProgetto, int changeId) => $"{numeroProgetto}-CHG{changeId}";

    private static int EnsureEntityId(int? id)
    {
        if (id.HasValue && id.Value > 0) return id.Value;
        var generated = (int)(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % int.MaxValue);
        return generated == 0 ? 1 : generated;
    }

    private List<ProjectDto> ApplyExportFilters(List<ProjectDto> projects, ProjectExportRequestDto request)
    {
        if (request?.Filters == null)
        {
            return projects;
        }

        try
        {
            var filters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(request.Filters);
            if (filters == null) return projects;

            foreach (var filter in filters)
            {
                switch (filter.Key.ToLowerInvariant())
                {
                    case "cliente":
                        var cliente = filter.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(cliente))
                        {
                            projects = projects.Where(p => p.Cliente.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();
                        }
                        break;
                    case "statoProgetto":
                        var stato = filter.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(stato))
                        {
                            projects = projects.Where(p => string.Equals(p.StatoProgetto.ToString(), stato, StringComparison.OrdinalIgnoreCase)).ToList();
                        }
                        break;
                    case "projectManager":
                        var pm = filter.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(pm))
                        {
                            projects = projects.Where(p => string.Equals(p.ProjectManager, pm, StringComparison.OrdinalIgnoreCase)).ToList();
                        }
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Impossibile applicare i filtri export, verranno ignorati");
        }

        return projects;
    }

    private byte[] GenerateCsv(IEnumerable<ProjectDto> projects)
    {
        var sb = new StringBuilder();
        sb.AppendLine("NumeroProgetto;NomeProgetto;Cliente;StatoProgetto;TotaleMq;TotaleFt;ValoreProgetto");
        foreach (var project in projects)
        {
            sb.AppendLine(string.Join(';', new[]
            {
                project.NumeroProgetto,
                Quote(project.NomeProgetto),
                Quote(project.Cliente),
                project.StatoProgetto.ToString(),
                project.QuantitaTotaleMq?.ToString("0.##") ?? "0",
                project.QuantitaTotaleFt?.ToString("0.##") ?? "0",
                project.ValoreProgetto?.ToString("0.##") ?? "0"
            }));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Quote(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(';') || value.Contains('"'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    /// <summary>
    /// Confronta due versioni di ProjectDto e genera una lista di modifiche
    /// </summary>
    private List<Dictionary<string, string>> CompareProjectVersions(ProjectDto oldVersion, ProjectDto newVersion)
    {
        var changes = new List<Dictionary<string, string>>();

        // Confronta le proprietà semplici
        CompareProperty("Cliente", oldVersion.Cliente, newVersion.Cliente, changes);
        CompareProperty("NomeProgetto", oldVersion.NomeProgetto, newVersion.NomeProgetto, changes);
        CompareProperty("Citta", oldVersion.Citta, newVersion.Citta, changes);
        CompareProperty("Stato", oldVersion.Stato, newVersion.Stato, changes);
        CompareProperty("TeamTecnico", oldVersion.TeamTecnico, newVersion.TeamTecnico, changes);
        CompareProperty("TeamAPL", oldVersion.TeamAPL, newVersion.TeamAPL, changes);
        CompareProperty("Sales", oldVersion.Sales, newVersion.Sales, changes);
        CompareProperty("ProjectManager", oldVersion.ProjectManager, newVersion.ProjectManager, changes);
        CompareProperty("TeamInstallazione", oldVersion.TeamInstallazione, newVersion.TeamInstallazione, changes);
        CompareProperty("VersioneWIC", oldVersion.VersioneWIC, newVersion.VersioneWIC, changes);
        CompareProperty("Note", oldVersion.Note, newVersion.Note, changes);

        // Confronta date (normalizzate senza timezone)
        CompareDateProperty("DataCreazione", oldVersion.DataCreazione, newVersion.DataCreazione, changes);
        CompareDateProperty("DataInizioInstallazione", oldVersion.DataInizioInstallazione, newVersion.DataInizioInstallazione, changes);
        CompareDateProperty("DataFineInstallazione", oldVersion.DataFineInstallazione, newVersion.DataFineInstallazione, changes);
        CompareDateProperty("UltimaModifica", oldVersion.UltimaModifica, newVersion.UltimaModifica, changes);

        // Confronta enum e bool
        CompareProperty("StatoProgetto", oldVersion.StatoProgetto.ToString(), newVersion.StatoProgetto.ToString(), changes);
        CompareProperty("IsInRitardo", oldVersion.IsInRitardo.ToString(), newVersion.IsInRitardo.ToString(), changes);

        // Confronta valori decimali (confronto numerico, non stringa)
        CompareDecimalProperty("ValoreProgetto", oldVersion.ValoreProgetto, newVersion.ValoreProgetto, changes);
        CompareDecimalProperty("MarginePrevisto", oldVersion.MarginePrevisto, newVersion.MarginePrevisto, changes);
        CompareDecimalProperty("CostiSostenuti", oldVersion.CostiSostenuti, newVersion.CostiSostenuti, changes);
        CompareDecimalProperty("QuantitaTotaleMq", oldVersion.QuantitaTotaleMq, newVersion.QuantitaTotaleMq, changes);
        CompareDecimalProperty("QuantitaTotaleFt", oldVersion.QuantitaTotaleFt, newVersion.QuantitaTotaleFt, changes);

        // Confronta collezioni (livelli e prodotti)
        CompareCollections("Livelli", oldVersion.Livelli, newVersion.Livelli, changes);
        CompareCollections("Prodotti", oldVersion.Prodotti, newVersion.Prodotti, changes);

        return changes;
    }

    /// <summary>
    /// Confronta una singola proprietà e aggiunge la modifica alla lista se diversa
    /// </summary>
    private void CompareProperty(string propertyName, string? oldValue, string? newValue, List<Dictionary<string, string>> changes)
    {
        var oldVal = oldValue ?? "";
        var newVal = newValue ?? "";

        if (oldVal != newVal)
        {
            changes.Add(new Dictionary<string, string>
            {
                ["Campo"] = propertyName,
                ["ValorePrecedente"] = oldVal,
                ["NuovoValore"] = newVal
            });
        }
    }

    /// <summary>
    /// Confronta una proprietà di tipo DateTime confrontando solo la parte data (yyyy-MM-dd), ignorando ora e timezone
    /// </summary>
    private void CompareDateProperty(string propertyName, DateTime? oldValue, DateTime? newValue, List<Dictionary<string, string>> changes)
    {
        // Se entrambi sono null, non c'è modifica
        if (!oldValue.HasValue && !newValue.HasValue)
            return;

        // Se uno è null e l'altro no, c'è modifica
        if (!oldValue.HasValue || !newValue.HasValue)
        {
            changes.Add(new Dictionary<string, string>
            {
                ["Campo"] = propertyName,
                ["ValorePrecedente"] = oldValue?.ToString("yyyy-MM-dd") ?? "",
                ["NuovoValore"] = newValue?.ToString("yyyy-MM-dd") ?? ""
            });
            return;
        }

        // Confronta solo la parte data (yyyy-MM-dd), ignorando completamente ora e timezone
        var oldDate = oldValue.Value;
        var newDate = newValue.Value;

        // Estrai solo la parte data (primi 10 caratteri: yyyy-MM-dd)
        var oldDateOnly = new DateTime(oldDate.Year, oldDate.Month, oldDate.Day);
        var newDateOnly = new DateTime(newDate.Year, newDate.Month, newDate.Day);

        // Confronta solo la data (ignora ora, minuti, secondi e timezone)
        if (oldDateOnly != newDateOnly)
        {
            changes.Add(new Dictionary<string, string>
            {
                ["Campo"] = propertyName,
                ["ValorePrecedente"] = oldDate.ToString("yyyy-MM-dd"),
                ["NuovoValore"] = newDate.ToString("yyyy-MM-dd")
            });
        }
    }

    /// <summary>
    /// Confronta una proprietà di tipo decimal ignorando le differenze di formato
    /// </summary>
    private void CompareDecimalProperty(string propertyName, decimal? oldValue, decimal? newValue, List<Dictionary<string, string>> changes)
    {
        // Normalizza i valori: considera null, 0, e valori vuoti come equivalenti
        var oldNormalized = NormalizeDecimal(oldValue);
        var newNormalized = NormalizeDecimal(newValue);

        // Se entrambi sono null/zero dopo la normalizzazione, non c'è modifica
        if (!oldNormalized.HasValue && !newNormalized.HasValue)
            return;

        // Se uno è null/zero e l'altro no, c'è modifica
        if (!oldNormalized.HasValue || !newNormalized.HasValue)
        {
            changes.Add(new Dictionary<string, string>
            {
                ["Campo"] = propertyName,
                ["ValorePrecedente"] = oldValue?.ToString() ?? "",
                ["NuovoValore"] = newValue?.ToString() ?? ""
            });
            return;
        }

        // Confronta i valori numerici (non le stringhe) con una tolleranza per arrotondamenti
        var difference = Math.Abs(oldNormalized.Value - newNormalized.Value);
        if (difference > 0.0001m) // Tolleranza per arrotondamenti floating point
        {
            changes.Add(new Dictionary<string, string>
            {
                ["Campo"] = propertyName,
                ["ValorePrecedente"] = oldValue?.ToString() ?? "",
                ["NuovoValore"] = newValue?.ToString() ?? ""
            });
        }
    }

    /// <summary>
    /// Normalizza un valore decimal: restituisce null se il valore è 0 o null
    /// </summary>
    private decimal? NormalizeDecimal(decimal? value)
    {
        if (!value.HasValue || value.Value == 0)
            return null;
        return value.Value;
    }

    /// <summary>
    /// Confronta due collezioni e registra le modifiche solo se ci sono differenze sostanziali
    /// </summary>
    private void CompareCollections<T>(string collectionName, List<T>? oldCollection, List<T>? newCollection, List<Dictionary<string, string>> changes)
    {
        var oldCount = oldCollection?.Count ?? 0;
        var newCount = newCollection?.Count ?? 0;

        if (oldCount != newCount)
        {
            changes.Add(new Dictionary<string, string>
            {
                ["Campo"] = collectionName,
                ["ValorePrecedente"] = oldCount.ToString(),
                ["NuovoValore"] = newCount.ToString()
            });
        }
        else if (oldCount > 0 && newCount > 0)
        {
            // Confronta il contenuto serializzato, ma normalizza prima per ignorare differenze di formato
            var oldJson = NormalizeJsonForComparison(JsonSerializer.Serialize(oldCollection ?? new List<T>()));
            var newJson = NormalizeJsonForComparison(JsonSerializer.Serialize(newCollection ?? new List<T>()));
            
            if (oldJson != newJson)
            {
                changes.Add(new Dictionary<string, string>
                {
                    ["Campo"] = $"{collectionName} (contenuto)",
                    ["ValorePrecedente"] = $"{oldCount} elementi",
                    ["NuovoValore"] = $"{newCount} elementi (contenuto modificato)"
                });
            }
        }
    }

    /// <summary>
    /// Normalizza un JSON per il confronto, rimuovendo differenze di formato (decimali, spazi, etc.)
    /// </summary>
    private string NormalizeJsonForComparison(string json)
    {
        if (string.IsNullOrEmpty(json))
            return "";

        // Rimuovi spazi e newline
        var normalized = json.Replace(" ", "").Replace("\n", "").Replace("\r", "");
        
        // Normalizza i decimali: "100000.0" -> "100000", "0.0" -> "0"
        // Usa regex per sostituire pattern come "100000.0" o "0.0" con la versione senza .0
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(\d+)\.0+(\D|$)", "$1$2");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(\d+)\.0+""", "$1\"");
        
        return normalized;
    }

    /// <summary>
    /// Genera le entry del ChangeLog confrontando due versioni del progetto
    /// </summary>
    private List<ChangeLogDto> GenerateChangeLogEntries(string numeroProgetto, ProjectDto oldVersion, ProjectDto newVersion, string? utente)
    {
        var changeLogEntries = new List<ChangeLogDto>();

        try
        {
            var changes = CompareProjectVersions(oldVersion, newVersion);

            if (changes.Count == 0)
            {
                _logger.LogDebug("Nessuna modifica rilevata per il progetto {NumeroProgetto}", numeroProgetto);
                return changeLogEntries;
            }

            // Crea un ChangeLog entry con tutte le modifiche nei dettagli
            // Questa entry verrà inclusa come collection nel payload SAP
            var changeLog = new ChangeLogDto
            {
                NumeroProgetto = numeroProgetto,
                Data = DateTime.UtcNow,
                Utente = utente ?? "System",
                Azione = "updated",
                Descrizione = $"Modificati {changes.Count} campo/i",
                Dettagli = changes.ToDictionary(
                    c => c["Campo"],
                    c => $"Da: {c["ValorePrecedente"]} -> A: {c["NuovoValore"]}"
                )
            };

            changeLogEntries.Add(changeLog);
            _logger.LogInformation("Generate {Count} modifiche per il ChangeLog del progetto {NumeroProgetto}", changes.Count, numeroProgetto);
        }
        catch (Exception ex)
        {
            // Log dell'errore ma non bloccare l'update del progetto
            _logger.LogError(ex, "Errore durante la generazione delle modifiche per il ChangeLog del progetto {NumeroProgetto}", numeroProgetto);
        }

        return changeLogEntries;
    }
}

