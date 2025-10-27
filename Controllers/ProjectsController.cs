using ADOTTA.Projects.Suite.Api.DTOs;
using ADOTTA.Projects.Suite.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ADOTTA.Projects.Suite.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    private string GetSessionId()
    {
        return Request.Headers["X-SAP-Session-Id"].ToString() ?? "";
    }

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
}

