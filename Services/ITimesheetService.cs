using ADOTTA.Projects.Suite.Api.DTOs;

namespace ADOTTA.Projects.Suite.Api.Services;

public interface ITimesheetService
{
    Task<List<TimesheetEntryDto>> GetAllEntriesAsync(string sessionId);
    Task<TimesheetEntryDto?> GetEntryByIdAsync(int id, string sessionId);
    Task<TimesheetEntryDto> CreateEntryAsync(TimesheetEntryDto entry, string sessionId);
    Task<TimesheetEntryDto> UpdateEntryAsync(int id, TimesheetEntryDto entry, string sessionId);
    Task DeleteEntryAsync(int id, string sessionId);
    Task<List<TimesheetEntryDto>> GetEntriesByProjectAsync(string numeroProgetto, string sessionId);
    Task<TimesheetOverviewDto> GetOverviewAsync(string? fromDate, string? toDate, string? utente, string sessionId);
    Task<TimesheetSummaryDto> GetSummaryAsync(string? fromDate, string? toDate, string? utente, string sessionId);
    Task<List<TimesheetEntryDto>> GetEntriesByUserAsync(string utente, string sessionId);
}

