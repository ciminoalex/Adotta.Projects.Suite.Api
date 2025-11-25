using ADOTTA.Projects.Suite.Api.DTOs;
using ADOTTA.Projects.Suite.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace ADOTTA.Projects.Suite.Api.Controllers;

[ApiController]
[Route("api/timesheet")]
public class TimesheetController : ControllerBase
{
    private readonly ITimesheetService _timesheetService;
    private readonly ILogger<TimesheetController> _logger;

    public TimesheetController(ITimesheetService timesheetService, ILogger<TimesheetController> logger)
    {
        _timesheetService = timesheetService;
        _logger = logger;
    }

    private string GetSessionId()
    {
        return Request.Headers["X-SAP-Session-Id"].ToString() ?? "";
    }

    [HttpGet]
    public async Task<ActionResult<List<TimesheetEntryDto>>> GetAll()
    {
        try
        {
            var entries = await _timesheetService.GetAllEntriesAsync(GetSessionId());
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all timesheet entries");
            return StatusCode(500, new { message = "Error retrieving timesheet entries", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TimesheetEntryDto>> GetById(int id)
    {
        try
        {
            var entry = await _timesheetService.GetEntryByIdAsync(id, GetSessionId());
            if (entry == null)
            {
                return NotFound(new { message = $"Timesheet entry '{id}' not found" });
            }
            return Ok(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timesheet entry: {Id}", id);
            return StatusCode(500, new { message = "Error retrieving timesheet entry", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<TimesheetEntryDto>> Create([FromBody] TimesheetEntryDto entry)
    {
        try
        {
            var created = await _timesheetService.CreateEntryAsync(entry, GetSessionId());
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating timesheet entry");
            return StatusCode(500, new { message = "Error creating timesheet entry", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TimesheetEntryDto>> Update(int id, [FromBody] TimesheetEntryDto entry)
    {
        try
        {
            var updated = await _timesheetService.UpdateEntryAsync(id, entry, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating timesheet entry: {Id}", id);
            return StatusCode(500, new { message = "Error updating timesheet entry", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _timesheetService.DeleteEntryAsync(id, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting timesheet entry: {Id}", id);
            return StatusCode(500, new { message = "Error deleting timesheet entry", error = ex.Message });
        }
    }

    [HttpGet("project/{numeroProgetto}")]
    public async Task<ActionResult<List<TimesheetEntryDto>>> GetByProject(string numeroProgetto)
    {
        try
        {
            var entries = await _timesheetService.GetEntriesByProjectAsync(numeroProgetto, GetSessionId());
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timesheet entries for project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error retrieving timesheet entries", error = ex.Message });
        }
    }

    [HttpGet("by-date-range")]
    public async Task<ActionResult<List<TimesheetEntryDto>>> GetByDateRange([FromQuery] string? startDate, [FromQuery] string? endDate)
    {
        try
        {
            var start = ParseDate(startDate);
            var end = ParseDate(endDate);
            var entries = await _timesheetService.GetEntriesByDateRangeAsync(start, end, GetSessionId());
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timesheet entries by date range");
            return StatusCode(500, new { message = "Error retrieving timesheet entries", error = ex.Message });
        }
    }

    [HttpGet("overview")]
    public async Task<ActionResult<TimesheetOverviewDto>> GetOverview([FromQuery] string? fromDate, [FromQuery] string? toDate, [FromQuery] string? utente)
    {
        try
        {
            var overview = await _timesheetService.GetOverviewAsync(fromDate, toDate, utente, GetSessionId());
            return Ok(overview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timesheet overview");
            return StatusCode(500, new { message = "Error retrieving timesheet overview", error = ex.Message });
        }
    }

    [HttpGet("summary")]
    public async Task<ActionResult<TimesheetSummaryDto>> GetSummary([FromQuery] string? fromDate, [FromQuery] string? toDate, [FromQuery] string? utente)
    {
        try
        {
            var summary = await _timesheetService.GetSummaryAsync(fromDate, toDate, utente, GetSessionId());
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timesheet summary");
            return StatusCode(500, new { message = "Error retrieving timesheet summary", error = ex.Message });
        }
    }

    [HttpGet("user/{utente}")]
    public async Task<ActionResult<List<TimesheetEntryDto>>> GetByUser(string utente)
    {
        try
        {
            var entries = await _timesheetService.GetEntriesByUserAsync(utente, GetSessionId());
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timesheet entries for user: {Utente}", utente);
            return StatusCode(500, new { message = "Error retrieving timesheet entries", error = ex.Message });
        }
    }

    [HttpGet("stats/by-project")]
    public async Task<ActionResult<List<TimesheetProjectStatsDto>>> GetStatsByProject()
    {
        try
        {
            var stats = await _timesheetService.GetStatsByProjectAsync(GetSessionId());
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timesheet stats by project");
            return StatusCode(500, new { message = "Error retrieving stats", error = ex.Message });
        }
    }

    [HttpGet("stats/by-user")]
    public async Task<ActionResult<List<TimesheetUserStatsDto>>> GetStatsByUser()
    {
        try
        {
            var stats = await _timesheetService.GetStatsByUserAsync(GetSessionId());
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timesheet stats by user");
            return StatusCode(500, new { message = "Error retrieving stats", error = ex.Message });
        }
    }

    [HttpGet("stats/daily")]
    public async Task<ActionResult<List<TimesheetDailyStatsDto>>> GetDailyStats([FromQuery] string date)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(date))
            {
                return BadRequest(new { message = "date query parameter is required" });
            }

            if (!DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedDate))
            {
                return BadRequest(new { message = "Invalid date format. Use ISO 8601." });
            }

            var stats = await _timesheetService.GetDailyStatsAsync(parsedDate, GetSessionId());
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily timesheet stats");
            return StatusCode(500, new { message = "Error retrieving stats", error = ex.Message });
        }
    }

    private static DateTime? ParseDate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed;
        }
        return null;
    }
}

