using ADOTTA.Projects.Suite.Api.DTOs;
using System.Text.Json;
using ADOTTA.Projects.Suite.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using ADOTTA.Projects.Suite.Api.Extensions;

namespace ADOTTA.Projects.Suite.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger, IServiceProvider serviceProvider)
    {
        _projectService = projectService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    private string GetSessionId() => HttpContext.GetSapSessionId();

    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAll()
    {
        try
        {
            var projects = await _projectService.GetAllProjectsAsync(GetSessionId());
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all projects");
            return StatusCode(500, new { message = "Error retrieving projects", error = ex.Message });
        }
    }

    [HttpGet("{numeroProgetto}")]
    public async Task<ActionResult<ProjectDto>> GetByCode(string numeroProgetto)
    {
        try
        {
            var project = await _projectService.GetProjectByCodeAsync(numeroProgetto, GetSessionId());
            if (project == null)
            {
                return NotFound(new { message = $"Project '{numeroProgetto}' not found" });
            }
            return Ok(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error retrieving project", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] ProjectDto project)
    {
        try
        {
            // Validate the project DTO
            var validator = _serviceProvider.GetRequiredService<IValidator<ProjectDto>>();
            var validationResult = await validator.ValidateAsync(project);
            if (!validationResult.IsValid)
            {
                // Convert PascalCase property names to camelCase for the error response
                var errors = validationResult.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(g.Key) ?? g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );
                return BadRequest(new { 
                    type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    title = "One or more validation errors occurred.",
                    status = 400,
                    errors = errors
                });
            }

            var created = await _projectService.CreateProjectAsync(project, GetSessionId());
            return CreatedAtAction(nameof(GetByCode), new { numeroProgetto = created.NumeroProgetto }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            return StatusCode(500, new { message = "Error creating project", error = ex.Message });
        }
    }

    [HttpPut("{numeroProgetto}")]
    public async Task<ActionResult<ProjectDto>> Update(string numeroProgetto, [FromBody] ProjectDto project)
    {
        try
        {
            var updated = await _projectService.UpdateProjectAsync(numeroProgetto, project, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error updating project", error = ex.Message });
        }
    }

    [HttpPatch("{numeroProgetto}")]
    public async Task<ActionResult<ProjectDto>> Patch(string numeroProgetto, [FromBody] JsonElement patchDocument)
    {
        try
        {
            var updated = await _projectService.PatchProjectAsync(numeroProgetto, patchDocument, GetSessionId());
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error patching project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error patching project", error = ex.Message });
        }
    }

    [HttpDelete("{numeroProgetto}")]
    public async Task<IActionResult> Delete(string numeroProgetto)
    {
        try
        {
            await _projectService.DeleteProjectAsync(numeroProgetto, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error deleting project", error = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<ProjectDto>>> Search([FromQuery] string q)
    {
        try
        {
            var projects = await _projectService.SearchProjectsAsync(q, GetSessionId());
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching projects with query: {Query}", q);
            return StatusCode(500, new { message = "Error searching projects", error = ex.Message });
        }
    }

    [HttpPost("filter")]
    public async Task<ActionResult<List<ProjectDto>>> Filter([FromBody] FilterRequestDto filter)
    {
        try
        {
            var projects = await _projectService.FilterProjectsAsync(filter, GetSessionId());
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering projects");
            return StatusCode(500, new { message = "Error filtering projects", error = ex.Message });
        }
    }

    [HttpGet("{numeroProgetto}/livelli")]
    public async Task<ActionResult<List<LivelloProgettoDto>>> GetLivelli(string numeroProgetto)
    {
        try
        {
            var livelli = await _projectService.GetLivelliAsync(numeroProgetto, GetSessionId());
            return Ok(livelli);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting livelli for project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error retrieving livelli", error = ex.Message });
        }
    }

    [HttpPost("{numeroProgetto}/livelli")]
    public async Task<ActionResult<LivelloProgettoDto>> CreateLivello(string numeroProgetto, [FromBody] LivelloProgettoDto livello)
    {
        try
        {
            var created = await _projectService.CreateLivelloAsync(numeroProgetto, livello, GetSessionId());
            return CreatedAtAction(nameof(GetLivelli), new { numeroProgetto }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating livello for project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error creating livello", error = ex.Message });
        }
    }

    [HttpPut("{numeroProgetto}/livelli/{livelloId}")]
    public async Task<ActionResult<LivelloProgettoDto>> UpdateLivello(string numeroProgetto, int livelloId, [FromBody] LivelloProgettoDto livello)
    {
        try
        {
            var updated = await _projectService.UpdateLivelloAsync(numeroProgetto, livelloId, livello, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating livello {LivelloId} for project: {NumeroProgetto}", livelloId, numeroProgetto);
            return StatusCode(500, new { message = "Error updating livello", error = ex.Message });
        }
    }

    [HttpDelete("{numeroProgetto}/livelli/{livelloId}")]
    public async Task<IActionResult> DeleteLivello(string numeroProgetto, int livelloId)
    {
        try
        {
            await _projectService.DeleteLivelloAsync(numeroProgetto, livelloId, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting livello: {LivelloId} for project: {NumeroProgetto}", livelloId, numeroProgetto);
            return StatusCode(500, new { message = "Error deleting livello", error = ex.Message });
        }
    }

    [HttpGet("{numeroProgetto}/prodotti")]
    public async Task<ActionResult<List<ProdottoProgettoDto>>> GetProdotti(string numeroProgetto)
    {
        try
        {
            var prodotti = await _projectService.GetProdottiAsync(numeroProgetto, GetSessionId());
            return Ok(prodotti);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prodotti for project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error retrieving prodotti", error = ex.Message });
        }
    }

    [HttpGet("{numeroProgetto}/livelli/{livelloId}/prodotti")]
    public async Task<ActionResult<List<ProdottoProgettoDto>>> GetProdottiByLivello(string numeroProgetto, int livelloId)
    {
        try
        {
            var prodotti = await _projectService.GetProdottiByLivelloAsync(numeroProgetto, livelloId, GetSessionId());
            return Ok(prodotti);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prodotti for livello {LivelloId} project {NumeroProgetto}", livelloId, numeroProgetto);
            return StatusCode(500, new { message = "Error retrieving prodotti", error = ex.Message });
        }
    }

    [HttpPost("{numeroProgetto}/livelli/{livelloId}/prodotti")]
    public async Task<ActionResult<ProdottoProgettoDto>> CreateProdottoInLivello(string numeroProgetto, int livelloId, [FromBody] ProdottoProgettoDto prodotto)
    {
        try
        {
            prodotto.LivelloId = livelloId;
            var created = await _projectService.CreateProdottoAsync(numeroProgetto, prodotto, GetSessionId());
            return CreatedAtAction(nameof(GetProdottiByLivello), new { numeroProgetto, livelloId }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating prodotto per livello {LivelloId} project {NumeroProgetto}", livelloId, numeroProgetto);
            return StatusCode(500, new { message = "Error creating prodotto", error = ex.Message });
        }
    }

    [HttpPost("{numeroProgetto}/prodotti")]
    public async Task<ActionResult<ProdottoProgettoDto>> CreateProdotto(string numeroProgetto, [FromBody] ProdottoProgettoDto prodotto)
    {
        try
        {
            var created = await _projectService.CreateProdottoAsync(numeroProgetto, prodotto, GetSessionId());
            return CreatedAtAction(nameof(GetProdotti), new { numeroProgetto }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating prodotto for project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error creating prodotto", error = ex.Message });
        }
    }

    [HttpPut("{numeroProgetto}/prodotti/{prodottoId}")]
    public async Task<ActionResult<ProdottoProgettoDto>> UpdateProdotto(string numeroProgetto, int prodottoId, [FromBody] ProdottoProgettoDto prodotto)
    {
        try
        {
            var updated = await _projectService.UpdateProdottoAsync(numeroProgetto, prodottoId, prodotto, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating prodotto {ProdottoId} for project: {NumeroProgetto}", prodottoId, numeroProgetto);
            return StatusCode(500, new { message = "Error updating prodotto", error = ex.Message });
        }
    }

    [HttpDelete("{numeroProgetto}/prodotti/{prodottoId}")]
    public async Task<IActionResult> DeleteProdotto(string numeroProgetto, int prodottoId)
    {
        try
        {
            await _projectService.DeleteProdottoAsync(numeroProgetto, prodottoId, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting prodotto: {ProdottoId} for project: {NumeroProgetto}", prodottoId, numeroProgetto);
            return StatusCode(500, new { message = "Error deleting prodotto", error = ex.Message });
        }
    }

    [HttpGet("{numeroProgetto}/storico")]
    public async Task<ActionResult<List<StoricoModificaDto>>> GetStorico(string numeroProgetto)
    {
        try
        {
            var storico = await _projectService.GetStoricoAsync(numeroProgetto, GetSessionId());
            return Ok(storico);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storico for project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error retrieving storico", error = ex.Message });
        }
    }

    [HttpPost("{numeroProgetto}/wic-snapshot")]
    public async Task<ActionResult<List<StoricoModificaDto>>> CreateWicSnapshot(string numeroProgetto)
    {
        try
        {
            var snapshot = await _projectService.CreateWicSnapshotAsync(numeroProgetto, GetSessionId());
            return Ok(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating WIC snapshot for project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error creating WIC snapshot", error = ex.Message });
        }
    }

    [HttpGet("{numeroProgetto}/messaggi")]
    public async Task<ActionResult<List<MessaggioProgettoDto>>> GetMessaggi(string numeroProgetto)
    {
        try
        {
            var messaggi = await _projectService.GetMessaggiAsync(numeroProgetto, GetSessionId());
            return Ok(messaggi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messaggi for project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error retrieving messaggi", error = ex.Message });
        }
    }

    [HttpPost("{numeroProgetto}/messaggi")]
    public async Task<ActionResult<MessaggioProgettoDto>> CreateMessaggio(string numeroProgetto, [FromBody] MessaggioProgettoDto messaggio)
    {
        try
        {
            var created = await _projectService.CreateMessaggioAsync(numeroProgetto, messaggio, GetSessionId());
            return CreatedAtAction(nameof(GetMessaggi), new { numeroProgetto }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating messaggio for project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error creating messaggio", error = ex.Message });
        }
    }

    [HttpPut("{numeroProgetto}/messaggi/{messaggioId}")]
    public async Task<ActionResult<MessaggioProgettoDto>> UpdateMessaggio(string numeroProgetto, string messaggioId, [FromBody] MessaggioProgettoDto messaggio)
    {
        try
        {
            var updated = await _projectService.UpdateMessaggioAsync(numeroProgetto, messaggioId, messaggio, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating messaggio {MessaggioId} for project: {NumeroProgetto}", messaggioId, numeroProgetto);
            return StatusCode(500, new { message = "Error updating messaggio", error = ex.Message });
        }
    }

    [HttpDelete("{numeroProgetto}/messaggi/{messaggioId}")]
    public async Task<IActionResult> DeleteMessaggio(string numeroProgetto, string messaggioId)
    {
        try
        {
            await _projectService.DeleteMessaggioAsync(numeroProgetto, messaggioId, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting messaggio {MessaggioId} for project: {NumeroProgetto}", messaggioId, numeroProgetto);
            return StatusCode(500, new { message = "Error deleting messaggio", error = ex.Message });
        }
    }

    [HttpGet("{numeroProgetto}/changelog")]
    public async Task<ActionResult<List<ChangeLogDto>>> GetChangeLog(string numeroProgetto)
    {
        try
        {
            var changelog = await _projectService.GetChangeLogAsync(numeroProgetto, GetSessionId());
            return Ok(changelog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting changelog for project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error retrieving changelog", error = ex.Message });
        }
    }

    [HttpPost("{numeroProgetto}/changelog")]
    public async Task<ActionResult<ChangeLogDto>> CreateChangeLog(string numeroProgetto, [FromBody] ChangeLogDto change)
    {
        try
        {
            var created = await _projectService.CreateChangeLogAsync(numeroProgetto, change, GetSessionId());
            return CreatedAtAction(nameof(GetChangeLog), new { numeroProgetto }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating changelog for project: {NumeroProgetto}", numeroProgetto);
            return StatusCode(500, new { message = "Error creating changelog", error = ex.Message });
        }
    }

    [HttpPost("export/{format}")]
    public async Task<IActionResult> Export(string format, [FromBody] ProjectExportRequestDto request)
    {
        try
        {
            var export = await _projectService.ExportProjectsAsync(format, request, GetSessionId());
            return File(export.Content, export.ContentType, export.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting projects");
            return StatusCode(500, new { message = "Error exporting projects", error = ex.Message });
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ProjectStatsDto>> GetStats()
    {
        try
        {
            var stats = await _projectService.GetProjectStatsAsync(GetSessionId());
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project stats");
            return StatusCode(500, new { message = "Error retrieving stats", error = ex.Message });
        }
    }

    [HttpGet("stats/by-status")]
    public async Task<ActionResult<List<ProjectStatsByStatusDto>>> GetStatsByStatus()
    {
        try
        {
            var stats = await _projectService.GetStatsByStatusAsync(GetSessionId());
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project stats by status");
            return StatusCode(500, new { message = "Error retrieving stats", error = ex.Message });
        }
    }

    [HttpGet("stats/by-month")]
    public async Task<ActionResult<List<ProjectStatsByMonthDto>>> GetStatsByMonth()
    {
        try
        {
            var stats = await _projectService.GetStatsByMonthAsync(GetSessionId());
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project stats by month");
            return StatusCode(500, new { message = "Error retrieving stats", error = ex.Message });
        }
    }
}

