using System.Text.Json;
using ADOTTA.Projects.Suite.Api.DTOs;

namespace ADOTTA.Projects.Suite.Api.Services;

public class TimesheetService : ITimesheetService
{
    private readonly ISAPServiceLayerClient _sapClient;
    private readonly ILogger<TimesheetService> _logger;

    public TimesheetService(ISAPServiceLayerClient sapClient, ILogger<TimesheetService> logger)
    {
        _sapClient = sapClient;
        _logger = logger;
    }

    public async Task<List<TimesheetEntryDto>> GetAllEntriesAsync(string sessionId)
    {
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("TIMESHEET", null, sessionId);
        return sapData.Select(MapToTimesheetDto).ToList();
    }

    public async Task<TimesheetEntryDto?> GetEntryByIdAsync(int id, string sessionId)
    {
        var filter = $"Code eq '{id}'";
        var entries = await _sapClient.GetRecordsAsync<JsonElement>("TIMESHEET", filter, sessionId);
        var entry = entries.FirstOrDefault();
        if (entry.ValueKind == JsonValueKind.Undefined) return null;
        return MapToTimesheetDto(entry);
    }

    public async Task<TimesheetEntryDto> CreateEntryAsync(TimesheetEntryDto entry, string sessionId)
    {
        var sapData = MapTimesheetToSap(entry);
        var result = await _sapClient.CreateRecordAsync<JsonElement>("TIMESHEET", sapData, sessionId);
        return MapToTimesheetDto(result);
    }

    public async Task<TimesheetEntryDto> UpdateEntryAsync(int id, TimesheetEntryDto entry, string sessionId)
    {
        var sapData = MapTimesheetToSap(entry);
        var result = await _sapClient.UpdateRecordAsync<JsonElement>("TIMESHEET", id.ToString(), sapData, sessionId);
        return MapToTimesheetDto(result);
    }

    public async Task DeleteEntryAsync(int id, string sessionId)
    {
        await _sapClient.DeleteRecordAsync("TIMESHEET", id.ToString(), sessionId);
    }

    public async Task<List<TimesheetEntryDto>> GetEntriesByProjectAsync(string numeroProgetto, string sessionId)
    {
        var filter = $"U_Progetto eq '{numeroProgetto}'";
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("TIMESHEET", filter, sessionId);
        return sapData.Select(MapToTimesheetDto).ToList();
    }

    public async Task<TimesheetOverviewDto> GetOverviewAsync(string? fromDate, string? toDate, string? utente, string sessionId)
    {
        var allEntries = await GetAllEntriesAsync(sessionId);

        // Apply filters if provided
        if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var from))
        {
            allEntries = allEntries.Where(e => e.DataRendicontazione >= from).ToList();
        }

        if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var to))
        {
            allEntries = allEntries.Where(e => e.DataRendicontazione <= to).ToList();
        }

        if (!string.IsNullOrEmpty(utente))
        {
            allEntries = allEntries.Where(e => e.Utente == utente).ToList();
        }

        // Group by project
        var projectGroups = allEntries.GroupBy(e => e.NumeroProgetto);
        var timesheets = projectGroups.Select(group => new TimesheetProjectDto
        {
            NumeroProgetto = group.Key,
            NomeProgetto = group.First().NomeProgetto,
            Cliente = group.First().Cliente,
            TotaleOre = group.Sum(e => e.OreLavorate),
            NumeroRendicontazioni = group.Count(),
            UltimaRendicontazione = group.Max(e => e.DataRendicontazione),
            Rendicontazioni = group.ToList()
        }).ToList();

        var summary = new TimesheetSummaryDto
        {
            TotaleOre = allEntries.Sum(e => e.OreLavorate),
            TotaleRendicontazioni = allEntries.Count,
            ProgettiRendicontati = projectGroups.Count(),
            MediaOrePerProgetto = projectGroups.Any() ? allEntries.Sum(e => e.OreLavorate) / projectGroups.Count() : 0
        };

        return new TimesheetOverviewDto
        {
            Timesheets = timesheets,
            Summary = summary
        };
    }

    public async Task<TimesheetSummaryDto> GetSummaryAsync(string? fromDate, string? toDate, string? utente, string sessionId)
    {
        var allEntries = await GetAllEntriesAsync(sessionId);

        // Apply filters
        if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out var from))
        {
            allEntries = allEntries.Where(e => e.DataRendicontazione >= from).ToList();
        }

        if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out var to))
        {
            allEntries = allEntries.Where(e => e.DataRendicontazione <= to).ToList();
        }

        if (!string.IsNullOrEmpty(utente))
        {
            allEntries = allEntries.Where(e => e.Utente == utente).ToList();
        }

        var projectCount = allEntries.Select(e => e.NumeroProgetto).Distinct().Count();

        return new TimesheetSummaryDto
        {
            TotaleOre = allEntries.Sum(e => e.OreLavorate),
            TotaleRendicontazioni = allEntries.Count,
            ProgettiRendicontati = projectCount,
            MediaOrePerProgetto = projectCount > 0 ? allEntries.Sum(e => e.OreLavorate) / projectCount : 0
        };
    }

    public async Task<List<TimesheetEntryDto>> GetEntriesByUserAsync(string utente, string sessionId)
    {
        var filter = $"U_Utente eq '{utente}'";
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>("TIMESHEET", filter, sessionId);
        return sapData.Select(MapToTimesheetDto).ToList();
    }

    private TimesheetEntryDto MapToTimesheetDto(JsonElement sapData)
    {
        return new TimesheetEntryDto
        {
            Id = sapData.TryGetProperty("Code", out var code) ? int.Parse(code.GetString() ?? "0") : 0,
            ProgettoId = sapData.TryGetProperty("U_Progetto", out var proj) ? proj.GetString() ?? "" : "",
            NumeroProgetto = sapData.TryGetProperty("U_NumeroProgetto", out var num) ? num.GetString() ?? "" : "",
            NomeProgetto = sapData.TryGetProperty("U_NomeProgetto", out var name) ? name.GetString() ?? "" : "",
            Cliente = sapData.TryGetProperty("U_Cliente", out var client) ? client.GetString() ?? "" : "",
            DataRendicontazione = sapData.TryGetProperty("U_DataRendicontazione", out var date) && DateTime.TryParse(date.GetString(), out var dt) ? dt : DateTime.MinValue,
            OreLavorate = sapData.TryGetProperty("U_OreLavorate", out var hours) ? hours.GetDouble() : 0,
            Note = sapData.TryGetProperty("U_Note", out var notes) ? notes.GetString() ?? "" : "",
            Utente = sapData.TryGetProperty("U_Utente", out var user) ? user.GetString() ?? "" : "",
            DataCreazione = sapData.TryGetProperty("U_DataCreazione", out var created) && DateTime.TryParse(created.GetString(), out var dtCreated) ? dtCreated : null,
            UltimaModifica = sapData.TryGetProperty("U_UltimaModifica", out var modified) && DateTime.TryParse(modified.GetString(), out var dtModified) ? dtModified : null
        };
    }

    private object MapTimesheetToSap(TimesheetEntryDto dto)
    {
        return new
        {
            Code = dto.Id.ToString(),
            U_Progetto = dto.ProgettoId,
            U_NumeroProgetto = dto.NumeroProgetto,
            U_NomeProgetto = dto.NomeProgetto,
            U_Cliente = dto.Cliente,
            U_DataRendicontazione = dto.DataRendicontazione.ToString("yyyy-MM-ddTHH:mm:ss"),
            U_OreLavorate = dto.OreLavorate,
            U_Note = dto.Note,
            U_Utente = dto.Utente,
            U_DataCreazione = dto.DataCreazione?.ToString("yyyy-MM-ddTHH:mm:ss") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
            U_UltimaModifica = dto.UltimaModifica?.ToString("yyyy-MM-ddTHH:mm:ss") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
        };
    }
}

