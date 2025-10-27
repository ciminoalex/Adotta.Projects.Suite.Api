using ADOTTA.Projects.Suite.Api.Models.Lookup;
using ADOTTA.Projects.Suite.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ADOTTA.Projects.Suite.Api.Controllers;

[ApiController]
[Route("api/lookup")]
public class LookupController : ControllerBase
{
    private readonly ILookupService _lookupService;
    private readonly ILogger<LookupController> _logger;

    public LookupController(ILookupService lookupService, ILogger<LookupController> logger)
    {
        _lookupService = lookupService;
        _logger = logger;
    }

    private string GetSessionId()
    {
        return Request.Headers["X-SAP-Session-Id"].ToString() ?? "";
    }

    #region Clienti

    [HttpGet("clienti")]
    public async Task<ActionResult<List<Cliente>>> GetAllClienti()
    {
        try
        {
            var clienti = await _lookupService.GetAllClientiAsync(GetSessionId());
            return Ok(clienti);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all clienti");
            return StatusCode(500, new { message = "Error retrieving clienti", error = ex.Message });
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

    #endregion

    #region Stati

    [HttpGet("stati")]
    public async Task<ActionResult<List<Stato>>> GetAllStati()
    {
        try
        {
            var stati = await _lookupService.GetAllStatiAsync(GetSessionId());
            return Ok(stati);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all stati");
            return StatusCode(500, new { message = "Error retrieving stati", error = ex.Message });
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
            _logger.LogError(ex, "Error getting squadre installazione");
            return StatusCode(500, new { message = "Error retrieving squadre installazione", error = ex.Message });
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
            _logger.LogError(ex, "Error getting prodotti master");
            return StatusCode(500, new { message = "Error retrieving prodotti master", error = ex.Message });
        }
    }

    #endregion
}

