using System.Text.Json;
using ADOTTA.Projects.Suite.Api.DTOs;

namespace ADOTTA.Projects.Suite.Api.Services;

public interface IProjectService
{
    Task<List<ProjectDto>> GetAllProjectsAsync(string sessionId);
    Task<ProjectDto?> GetProjectByCodeAsync(string numeroProgetto, string sessionId);
    Task<ProjectDto> CreateProjectAsync(ProjectDto project, string sessionId);
    Task<ProjectDto> UpdateProjectAsync(string numeroProgetto, ProjectDto project, string sessionId);
    Task<ProjectDto> PatchProjectAsync(string numeroProgetto, JsonElement patchDocument, string sessionId);
    Task DeleteProjectAsync(string numeroProgetto, string sessionId);
    Task<List<ProjectDto>> SearchProjectsAsync(string searchTerm, string sessionId);
    Task<List<ProjectDto>> FilterProjectsAsync(FilterRequestDto filter, string sessionId);
    Task<List<LivelloProgettoDto>> GetLivelliAsync(string numeroProgetto, string sessionId);
    Task<LivelloProgettoDto> CreateLivelloAsync(string numeroProgetto, LivelloProgettoDto livello, string sessionId);
    Task<LivelloProgettoDto> UpdateLivelloAsync(string numeroProgetto, int livelloId, LivelloProgettoDto livello, string sessionId);
    Task DeleteLivelloAsync(string numeroProgetto, int livelloId, string sessionId);
    Task<List<ProdottoProgettoDto>> GetProdottiAsync(string numeroProgetto, string sessionId);
    Task<List<ProdottoProgettoDto>> GetProdottiByLivelloAsync(string numeroProgetto, int livelloId, string sessionId);
    Task<ProdottoProgettoDto> CreateProdottoAsync(string numeroProgetto, ProdottoProgettoDto prodotto, string sessionId);
    Task<ProdottoProgettoDto> UpdateProdottoAsync(string numeroProgetto, int prodottoId, ProdottoProgettoDto prodotto, string sessionId);
    Task DeleteProdottoAsync(string numeroProgetto, int prodottoId, string sessionId);
    Task<List<StoricoModificaDto>> GetStoricoAsync(string numeroProgetto, string sessionId);
    Task<List<StoricoModificaDto>> CreateWicSnapshotAsync(string numeroProgetto, string sessionId);
    Task<List<MessaggioProgettoDto>> GetMessaggiAsync(string numeroProgetto, string sessionId);
    Task<MessaggioProgettoDto> CreateMessaggioAsync(string numeroProgetto, MessaggioProgettoDto messaggio, string sessionId);
    Task<MessaggioProgettoDto> UpdateMessaggioAsync(string numeroProgetto, string messaggioId, MessaggioProgettoDto messaggio, string sessionId);
    Task DeleteMessaggioAsync(string numeroProgetto, string messaggioId, string sessionId);
    Task<List<ChangeLogDto>> GetChangeLogAsync(string numeroProgetto, string sessionId);
    Task<ChangeLogDto> CreateChangeLogAsync(string numeroProgetto, ChangeLogDto change, string sessionId);
    Task<ProjectExportResultDto> ExportProjectsAsync(string format, ProjectExportRequestDto request, string sessionId);
    Task<ProjectStatsDto> GetProjectStatsAsync(string sessionId);
    Task<List<ProjectStatsByStatusDto>> GetStatsByStatusAsync(string sessionId);
    Task<List<ProjectStatsByMonthDto>> GetStatsByMonthAsync(string sessionId);
}

