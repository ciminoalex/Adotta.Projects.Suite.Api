using ADOTTA.Projects.Suite.Api.Models.Lookup;

namespace ADOTTA.Projects.Suite.Api.Services;

public interface ILookupService
{
    Task<List<Cliente>> GetAllClientiAsync(string sessionId);
    Task<Cliente?> GetClienteByIdAsync(string id, string sessionId);
    Task<List<Stato>> GetAllStatiAsync(string sessionId);
    Task<List<Citta>> GetAllCittaAsync(string sessionId, string? statoId = null);
    Task<List<TeamTecnico>> GetAllTeamTecniciAsync(string sessionId);
    Task<List<TeamAPL>> GetAllTeamAPLAsync(string sessionId);
    Task<List<Sales>> GetAllSalesAsync(string sessionId);
    Task<List<ProjectManager>> GetAllProjectManagersAsync(string sessionId);
    Task<List<SquadraInstallazione>> GetAllSquadreInstallazioneAsync(string sessionId);
    Task<List<ProdottoMaster>> GetAllProdottiMasterAsync(string sessionId, string? categoria = null);
}

