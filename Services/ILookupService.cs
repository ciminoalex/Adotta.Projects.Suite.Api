using ADOTTA.Projects.Suite.Api.Models.Lookup;

namespace ADOTTA.Projects.Suite.Api.Services;

public interface ILookupService
{
    Task<List<Cliente>> GetAllClientiAsync(string sessionId);
    Task<Cliente?> GetClienteByIdAsync(string id, string sessionId);
    Task<List<Cliente>> SearchClientiAsync(string query, string sessionId);
    Task<Cliente> CreateClienteAsync(Cliente cliente, string sessionId);
    Task<Cliente> UpdateClienteAsync(string id, Cliente cliente, string sessionId);
    Task DeleteClienteAsync(string id, string sessionId);
    Task<List<Stato>> GetAllStatiAsync(string sessionId);
    Task<Stato?> GetStatoByIdAsync(string id, string sessionId);
    Task<List<Citta>> GetAllCittaAsync(string sessionId, string? statoId = null);
    Task<Citta?> GetCittaByIdAsync(string id, string sessionId);
    Task<Citta> CreateCittaAsync(Citta citta, string sessionId);
    Task<Citta> UpdateCittaAsync(string id, Citta citta, string sessionId);
    Task DeleteCittaAsync(string id, string sessionId);
    Task<List<TeamTecnico>> GetAllTeamTecniciAsync(string sessionId);
    Task<TeamTecnico?> GetTeamTecnicoByIdAsync(string id, string sessionId);
    Task<TeamTecnico> CreateTeamTecnicoAsync(TeamTecnico team, string sessionId);
    Task<TeamTecnico> UpdateTeamTecnicoAsync(string id, TeamTecnico team, string sessionId);
    Task DeleteTeamTecnicoAsync(string id, string sessionId);
    Task<List<TeamAPL>> GetAllTeamAPLAsync(string sessionId);
    Task<TeamAPL?> GetTeamAPLByIdAsync(string id, string sessionId);
    Task<TeamAPL> CreateTeamAPLAsync(TeamAPL team, string sessionId);
    Task<TeamAPL> UpdateTeamAPLAsync(string id, TeamAPL team, string sessionId);
    Task DeleteTeamAPLAsync(string id, string sessionId);
    Task<List<Sales>> GetAllSalesAsync(string sessionId);
    Task<Sales?> GetSalesByIdAsync(string id, string sessionId);
    Task<Sales> CreateSalesAsync(Sales sales, string sessionId);
    Task<Sales> UpdateSalesAsync(string id, Sales sales, string sessionId);
    Task DeleteSalesAsync(string id, string sessionId);
    Task<List<ProjectManager>> GetAllProjectManagersAsync(string sessionId);
    Task<ProjectManager?> GetProjectManagerByIdAsync(string id, string sessionId);
    Task<ProjectManager> CreateProjectManagerAsync(ProjectManager manager, string sessionId);
    Task<ProjectManager> UpdateProjectManagerAsync(string id, ProjectManager manager, string sessionId);
    Task DeleteProjectManagerAsync(string id, string sessionId);
    Task<List<SquadraInstallazione>> GetAllSquadreInstallazioneAsync(string sessionId);
    Task<SquadraInstallazione?> GetSquadraInstallazioneByIdAsync(string id, string sessionId);
    Task<SquadraInstallazione> CreateSquadraInstallazioneAsync(SquadraInstallazione squadra, string sessionId);
    Task<SquadraInstallazione> UpdateSquadraInstallazioneAsync(string id, SquadraInstallazione squadra, string sessionId);
    Task DeleteSquadraInstallazioneAsync(string id, string sessionId);
    Task<List<ProdottoMaster>> GetAllProdottiMasterAsync(string sessionId, string? categoria = null);
    Task<ProdottoMaster?> GetProdottoMasterByIdAsync(string id, string sessionId);
    Task<ProdottoMaster> CreateProdottoMasterAsync(ProdottoMaster prodotto, string sessionId);
    Task<ProdottoMaster> UpdateProdottoMasterAsync(string id, ProdottoMaster prodotto, string sessionId);
    Task DeleteProdottoMasterAsync(string id, string sessionId);
}

