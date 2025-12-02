using ADOTTA.Projects.Suite.Api.DTOs;
using ADOTTA.Projects.Suite.Api.Models.Lookup;
using ADOTTA.Projects.Suite.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ADOTTA.Projects.Suite.Api.Extensions;
using System.Net;

namespace ADOTTA.Projects.Suite.Api.Controllers;

[ApiController]
[Route("api/lookup")]
[Authorize]
public class LookupController : ControllerBase
{
    private readonly ILookupService _lookupService;
    private readonly ILogger<LookupController> _logger;

    public LookupController(ILookupService lookupService, ILogger<LookupController> logger)
    {
        _lookupService = lookupService;
        _logger = logger;
    }

    private string GetSessionId() => HttpContext.GetSapSessionId();

    #region Clienti

    [HttpGet("clienti")]
    public async Task<ActionResult<PagedResultDto<Cliente>>> GetClienti(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 1000) pageSize = 1000; // Limite massimo

            var result = await _lookupService.GetClientiPagedAsync(GetSessionId(), page, pageSize, search, sortBy, sortDirection);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting clienti");
            return StatusCode(500, new { message = "Error retrieving clienti", error = ex.Message });
        }
    }

    [HttpGet("clienti/search")]
    public async Task<ActionResult<List<Cliente>>> SearchClienti([FromQuery] string q)
    {
        try
        {
            var clienti = await _lookupService.SearchClientiAsync(q, GetSessionId());
            return Ok(clienti);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching clienti");
            return StatusCode(500, new { message = "Error searching clienti", error = ex.Message });
        }
    }

    [HttpGet("clienti/{id}")]
    public async Task<ActionResult<Cliente>> GetClienteById(string id)
    {
        try
        {
            var cliente = await _lookupService.GetClienteByIdAsync(id, GetSessionId());
            if (cliente == null)
            {
                return NotFound(new { message = $"Cliente '{id}' not found" });
            }
            return Ok(cliente);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cliente: {Id}", id);
            return StatusCode(500, new { message = "Error retrieving cliente", error = ex.Message });
        }
    }

    [HttpPost("clienti")]
    public async Task<ActionResult<Cliente>> CreateCliente([FromBody] Cliente cliente)
    {
        try
        {
            var created = await _lookupService.CreateClienteAsync(cliente, GetSessionId());
            return CreatedAtAction(nameof(GetClienteById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cliente");
            return StatusCode(500, new { message = "Error creating cliente", error = ex.Message });
        }
    }

    [HttpPut("clienti/{id}")]
    public async Task<ActionResult<Cliente>> UpdateCliente(string id, [FromBody] Cliente cliente)
    {
        try
        {
            var updated = await _lookupService.UpdateClienteAsync(id, cliente, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cliente {ClienteId}", id);
            return StatusCode(500, new { message = "Error updating cliente", error = ex.Message });
        }
    }

    [HttpDelete("clienti/{id}")]
    public async Task<IActionResult> DeleteCliente(string id)
    {
        try
        {
            await _lookupService.DeleteClienteAsync(id, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting cliente {ClienteId}", id);
            return StatusCode(500, new { message = "Error deleting cliente", error = ex.Message });
        }
    }

    #endregion

    #region Stati

    [HttpGet("stati")]
    public async Task<ActionResult<PagedResultDto<Stato>>> GetStati([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100; // Limite massimo

            var result = await _lookupService.GetStatiPagedAsync(GetSessionId(), page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stati");
            return StatusCode(500, new { message = "Error retrieving stati", error = ex.Message });
        }
    }

    [HttpGet("stati/{id}")]
    public async Task<ActionResult<Stato>> GetStatoById(string id)
    {
        try
        {
            var stato = await _lookupService.GetStatoByIdAsync(id, GetSessionId());
            if (stato == null)
            {
                return NotFound(new { message = $"Stato '{id}' not found" });
            }
            return Ok(stato);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stato {Id}", id);
            return StatusCode(500, new { message = "Error retrieving stato", error = ex.Message });
        }
    }

    #endregion

    #region Città

    [HttpGet("citta")]
    public async Task<ActionResult<List<Citta>>> GetAllCitta([FromQuery] string? statoId)
    {
        try
        {
            var citta = await _lookupService.GetAllCittaAsync(GetSessionId(), statoId);
            return Ok(citta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting citta");
            return StatusCode(500, new { message = "Error retrieving citta", error = ex.Message });
        }
    }

    [HttpGet("citta/{id}")]
    public async Task<ActionResult<Citta>> GetCittaById(string id)
    {
        try
        {
            var citta = await _lookupService.GetCittaByIdAsync(id, GetSessionId());
            if (citta == null)
            {
                return NotFound(new { message = $"Citta '{id}' not found" });
            }
            return Ok(citta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting citta {Id}", id);
            return StatusCode(500, new { message = "Error retrieving citta", error = ex.Message });
        }
    }

    [HttpPost("citta")]
    public async Task<ActionResult<Citta>> CreateCitta([FromBody] Citta citta)
    {
        try
        {
            var created = await _lookupService.CreateCittaAsync(citta, GetSessionId());
            return CreatedAtAction(nameof(GetCittaById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating citta");
            return StatusCode(500, new { message = "Error creating citta", error = ex.Message });
        }
    }

    [HttpPut("citta/{id}")]
    public async Task<ActionResult<Citta>> UpdateCitta(string id, [FromBody] Citta citta)
    {
        try
        {
            var updated = await _lookupService.UpdateCittaAsync(id, citta, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating citta {Id}", id);
            return StatusCode(500, new { message = "Error updating citta", error = ex.Message });
        }
    }

    [HttpDelete("citta/{id}")]
    public async Task<IActionResult> DeleteCitta(string id)
    {
        try
        {
            await _lookupService.DeleteCittaAsync(id, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting citta {Id}", id);
            return StatusCode(500, new { message = "Error deleting citta", error = ex.Message });
        }
    }

    #endregion

    #region Team Tecnici

    [HttpGet("team-tecnici")]
    public async Task<ActionResult<List<TeamTecnico>>> GetAllTeamTecnici()
    {
        try
        {
            var teams = await _lookupService.GetAllTeamTecniciAsync(GetSessionId());
            return Ok(teams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team tecnici");
            return StatusCode(500, new { message = "Error retrieving team tecnici", error = ex.Message });
        }
    }

    [HttpGet("team-tecnici/{id}")]
    public async Task<ActionResult<TeamTecnico>> GetTeamTecnicoById(string id)
    {
        try
        {
            var team = await _lookupService.GetTeamTecnicoByIdAsync(id, GetSessionId());
            if (team == null)
            {
                return NotFound(new { message = $"Team tecnico '{id}' not found" });
            }
            return Ok(team);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team tecnico {Id}", id);
            return StatusCode(500, new { message = "Error retrieving team tecnico", error = ex.Message });
        }
    }

    [HttpPost("team-tecnici")]
    public async Task<ActionResult<TeamTecnico>> CreateTeamTecnico([FromBody] TeamTecnico team)
    {
        try
        {
            var created = await _lookupService.CreateTeamTecnicoAsync(team, GetSessionId());
            return CreatedAtAction(nameof(GetTeamTecnicoById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating team tecnico");
            return StatusCode(500, new { message = "Error creating team tecnico", error = ex.Message });
        }
    }

    [HttpPut("team-tecnici/{id}")]
    public async Task<ActionResult<TeamTecnico>> UpdateTeamTecnico(string id, [FromBody] TeamTecnico team)
    {
        try
        {
            var updated = await _lookupService.UpdateTeamTecnicoAsync(id, team, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating team tecnico {Id}", id);
            return StatusCode(500, new { message = "Error updating team tecnico", error = ex.Message });
        }
    }

    [HttpDelete("team-tecnici/{id}")]
    public async Task<IActionResult> DeleteTeamTecnico(string id)
    {
        try
        {
            await _lookupService.DeleteTeamTecnicoAsync(id, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting team tecnico {Id}", id);
            return StatusCode(500, new { message = "Error deleting team tecnico", error = ex.Message });
        }
    }

    #endregion

    #region Team APL

    [HttpGet("team-apl")]
    public async Task<ActionResult<List<TeamAPL>>> GetAllTeamAPL()
    {
        try
        {
            var teams = await _lookupService.GetAllTeamAPLAsync(GetSessionId());
            return Ok(teams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team APL");
            return StatusCode(500, new { message = "Error retrieving team APL", error = ex.Message });
        }
    }

    [HttpGet("team-apl/{id}")]
    public async Task<ActionResult<TeamAPL>> GetTeamAPLById(string id)
    {
        try
        {
            var team = await _lookupService.GetTeamAPLByIdAsync(id, GetSessionId());
            if (team == null)
            {
                return NotFound(new { message = $"Team APL '{id}' not found" });
            }
            return Ok(team);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team APL {Id}", id);
            return StatusCode(500, new { message = "Error retrieving team APL", error = ex.Message });
        }
    }

    [HttpPost("team-apl")]
    public async Task<ActionResult<TeamAPL>> CreateTeamAPL([FromBody] TeamAPL team)
    {
        try
        {
            var created = await _lookupService.CreateTeamAPLAsync(team, GetSessionId());
            return CreatedAtAction(nameof(GetTeamAPLById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating team APL");
            return StatusCode(500, new { message = "Error creating team APL", error = ex.Message });
        }
    }

    [HttpPut("team-apl/{id}")]
    public async Task<ActionResult<TeamAPL>> UpdateTeamAPL(string id, [FromBody] TeamAPL team)
    {
        try
        {
            var updated = await _lookupService.UpdateTeamAPLAsync(id, team, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating team APL {Id}", id);
            return StatusCode(500, new { message = "Error updating team APL", error = ex.Message });
        }
    }

    [HttpDelete("team-apl/{id}")]
    public async Task<IActionResult> DeleteTeamAPL(string id)
    {
        try
        {
            await _lookupService.DeleteTeamAPLAsync(id, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting team APL {Id}", id);
            return StatusCode(500, new { message = "Error deleting team APL", error = ex.Message });
        }
    }

    #endregion

    #region Sales

    [HttpGet("sales")]
    public async Task<ActionResult<List<Sales>>> GetAllSales()
    {
        try
        {
            var sales = await _lookupService.GetAllSalesAsync(GetSessionId());
            return Ok(sales);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales");
            return StatusCode(500, new { message = "Error retrieving sales", error = ex.Message });
        }
    }

    [HttpGet("sales/{id}")]
    public async Task<ActionResult<Sales>> GetSalesById(string id)
    {
        try
        {
            var sales = await _lookupService.GetSalesByIdAsync(id, GetSessionId());
            if (sales == null)
            {
                return NotFound(new { message = $"Sales '{id}' not found" });
            }
            return Ok(sales);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales {Id}", id);
            return StatusCode(500, new { message = "Error retrieving sales", error = ex.Message });
        }
    }

    [HttpPost("sales")]
    public async Task<ActionResult<Sales>> CreateSales([FromBody] Sales sales)
    {
        try
        {
            var created = await _lookupService.CreateSalesAsync(sales, GetSessionId());
            return CreatedAtAction(nameof(GetSalesById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sales");
            return StatusCode(500, new { message = "Error creating sales", error = ex.Message });
        }
    }

    [HttpPut("sales/{id}")]
    public async Task<ActionResult<Sales>> UpdateSales(string id, [FromBody] Sales sales)
    {
        try
        {
            var updated = await _lookupService.UpdateSalesAsync(id, sales, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sales {Id}", id);
            return StatusCode(500, new { message = "Error updating sales", error = ex.Message });
        }
    }

    [HttpDelete("sales/{id}")]
    public async Task<IActionResult> DeleteSales(string id)
    {
        try
        {
            await _lookupService.DeleteSalesAsync(id, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sales {Id}", id);
            return StatusCode(500, new { message = "Error deleting sales", error = ex.Message });
        }
    }

    #endregion

    #region Project Managers

    [HttpGet("project-managers")]
    public async Task<ActionResult<List<ProjectManager>>> GetAllProjectManagers()
    {
        try
        {
            var pms = await _lookupService.GetAllProjectManagersAsync(GetSessionId());
            return Ok(pms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project managers");
            return StatusCode(500, new { message = "Error retrieving project managers", error = ex.Message });
        }
    }

    [HttpGet("project-managers/{id}")]
    public async Task<ActionResult<ProjectManager>> GetProjectManagerById(string id)
    {
        try
        {
            var manager = await _lookupService.GetProjectManagerByIdAsync(id, GetSessionId());
            if (manager == null)
            {
                return NotFound(new { message = $"Project manager '{id}' not found" });
            }
            return Ok(manager);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project manager {Id}", id);
            return StatusCode(500, new { message = "Error retrieving project manager", error = ex.Message });
        }
    }

    [HttpPost("project-managers")]
    public async Task<ActionResult<ProjectManager>> CreateProjectManager([FromBody] ProjectManager manager)
    {
        try
        {
            var created = await _lookupService.CreateProjectManagerAsync(manager, GetSessionId());
            return CreatedAtAction(nameof(GetProjectManagerById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project manager");
            return StatusCode(500, new { message = "Error creating project manager", error = ex.Message });
        }
    }

    [HttpPut("project-managers/{id}")]
    public async Task<ActionResult<ProjectManager>> UpdateProjectManager(string id, [FromBody] ProjectManager manager)
    {
        try
        {
            var updated = await _lookupService.UpdateProjectManagerAsync(id, manager, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project manager {Id}", id);
            return StatusCode(500, new { message = "Error updating project manager", error = ex.Message });
        }
    }

    [HttpDelete("project-managers/{id}")]
    public async Task<IActionResult> DeleteProjectManager(string id)
    {
        try
        {
            await _lookupService.DeleteProjectManagerAsync(id, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project manager {Id}", id);
            return StatusCode(500, new { message = "Error deleting project manager", error = ex.Message });
        }
    }

    #endregion

    #region Squadre Installazione

    [HttpGet("squadre-installazione")]
    public async Task<ActionResult<List<SquadraInstallazione>>> GetAllSquadreInstallazione()
    {
        try
        {
            var squadre = await _lookupService.GetAllSquadreInstallazioneAsync(GetSessionId());
            return Ok(squadre);
        }
        catch (Exception ex)
        {
            return this.HandleSapError<List<SquadraInstallazione>>(ex, _logger, "getting squadre installazione");
        }
    }

    [HttpGet("squadre-installazione/{id}")]
    public async Task<ActionResult<SquadraInstallazione>> GetSquadraInstallazioneById(string id)
    {
        try
        {
            var squadra = await _lookupService.GetSquadraInstallazioneByIdAsync(id, GetSessionId());
            if (squadra == null)
            {
                return NotFound(new { message = $"Squadra installazione '{id}' not found" });
            }
            return Ok(squadra);
        }
        catch (Exception ex)
        {
            return this.HandleSapError<SquadraInstallazione>(ex, _logger, $"getting squadra installazione {id}");
        }
    }

    [HttpPost("squadre-installazione")]
    public async Task<ActionResult<SquadraInstallazione>> CreateSquadraInstallazione([FromBody] SquadraInstallazione squadra)
    {
        try
        {
            var created = await _lookupService.CreateSquadraInstallazioneAsync(squadra, GetSessionId());
            return CreatedAtAction(nameof(GetSquadraInstallazioneById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating squadra installazione");
            return StatusCode(500, new { message = "Error creating squadra installazione", error = ex.Message });
        }
    }

    [HttpPut("squadre-installazione/{id}")]
    public async Task<ActionResult<SquadraInstallazione>> UpdateSquadraInstallazione(string id, [FromBody] SquadraInstallazione squadra)
    {
        try
        {
            var updated = await _lookupService.UpdateSquadraInstallazioneAsync(id, squadra, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating squadra installazione {Id}", id);
            return StatusCode(500, new { message = "Error updating squadra installazione", error = ex.Message });
        }
    }

    [HttpDelete("squadre-installazione/{id}")]
    public async Task<IActionResult> DeleteSquadraInstallazione(string id)
    {
        try
        {
            await _lookupService.DeleteSquadraInstallazioneAsync(id, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting squadra installazione {Id}", id);
            return StatusCode(500, new { message = "Error deleting squadra installazione", error = ex.Message });
        }
    }

    #endregion

    #region Prodotti Master

    [HttpGet("prodotti-master")]
    public async Task<ActionResult<List<ProdottoMaster>>> GetAllProdottiMaster([FromQuery] string? categoria)
    {
        try
        {
            var prodotti = await _lookupService.GetAllProdottiMasterAsync(GetSessionId(), categoria);
            return Ok(prodotti);
        }
        catch (Exception ex)
        {
            return this.HandleSapError<List<ProdottoMaster>>(ex, _logger, "getting prodotti master");
        }
    }

    [HttpGet("prodotti-master/{id}")]
    public async Task<ActionResult<ProdottoMaster>> GetProdottoMasterById(string id)
    {
        try
        {
            var prodotto = await _lookupService.GetProdottoMasterByIdAsync(id, GetSessionId());
            if (prodotto == null)
            {
                return NotFound(new { message = $"Prodotto master '{id}' not found" });
            }
            return Ok(prodotto);
        }
        catch (Exception ex)
        {
            return this.HandleSapError<ProdottoMaster>(ex, _logger, $"getting prodotto master {id}");
        }
    }

    [HttpPost("prodotti-master")]
    public async Task<ActionResult<ProdottoMaster>> CreateProdottoMaster([FromBody] ProdottoMaster prodotto)
    {
        try
        {
            var created = await _lookupService.CreateProdottoMasterAsync(prodotto, GetSessionId());
            return CreatedAtAction(nameof(GetProdottoMasterById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating prodotto master");
            return StatusCode(500, new { message = "Error creating prodotto master", error = ex.Message });
        }
    }

    [HttpPut("prodotti-master/{id}")]
    public async Task<ActionResult<ProdottoMaster>> UpdateProdottoMaster(string id, [FromBody] ProdottoMaster prodotto)
    {
        try
        {
            var updated = await _lookupService.UpdateProdottoMasterAsync(id, prodotto, GetSessionId());
            return Ok(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating prodotto master {Id}", id);
            return StatusCode(500, new { message = "Error updating prodotto master", error = ex.Message });
        }
    }

    [HttpDelete("prodotti-master/{id}")]
    public async Task<IActionResult> DeleteProdottoMaster(string id)
    {
        try
        {
            await _lookupService.DeleteProdottoMasterAsync(id, GetSessionId());
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting prodotto master {Id}", id);
            return StatusCode(500, new { message = "Error deleting prodotto master", error = ex.Message });
        }
    }

    #endregion
}

