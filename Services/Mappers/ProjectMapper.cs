using ADOTTA.Projects.Suite.Api.Models;
using ADOTTA.Projects.Suite.Api.DTOs;
using System.Text.Json;

namespace ADOTTA.Projects.Suite.Api.Services.Mappers;

public static class ProjectMapper
{
    public static object MapProjectToSapUDO(ProjectDto dto)
    {
        return new
        {
            Code = dto.NumeroProgetto,
            Name = dto.NomeProgetto,
            U_Cliente = dto.Cliente,
            U_Citta = dto.Citta,
            U_Stato = dto.Stato,
            U_TeamTecnico = dto.TeamTecnico ?? "",
            U_TeamAPL = dto.TeamAPL ?? "",
            U_Sales = dto.Sales ?? "",
            U_ProjectManager = dto.ProjectManager ?? "",
            U_TeamInstallazione = dto.TeamInstallazione ?? "",
            U_DataCreazione = dto.DataCreazione.ToString("yyyy-MM-ddTHH:mm:ss"),
            U_DataInizioInstall = dto.DataInizioInstallazione?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "",
            U_DataFineInstall = dto.DataFineInstallazione?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "",
            U_VersioneWIC = dto.VersioneWIC ?? "",
            U_UltimaModifica = dto.UltimaModifica?.ToString("yyyy-MM-ddTHH:mm:ss") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
            U_StatoProgetto = dto.StatoProgetto ?? "",
            U_ValoreProgetto = dto.ValoreProgetto ?? 0,
            U_MarginePrevisto = dto.MarginePrevisto ?? 0,
            U_CostiSostenuti = dto.CostiSostenuti ?? 0,
            U_Note = dto.Note ?? "",
            U_IsInRitardo = dto.IsInRitardo ? "Y" : "N"
        };
    }

    public static ProjectDto MapSapUDOToProject(JsonElement sapData)
    {
        return new ProjectDto
        {
            NumeroProgetto = sapData.TryGetProperty("Code", out var code) ? code.GetString() ?? "" : "",
            NomeProgetto = sapData.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : "",
            Cliente = sapData.TryGetProperty("U_Cliente", out var client) ? client.GetString() ?? "" : "",
            Citta = sapData.TryGetProperty("U_Citta", out var city) ? city.GetString() ?? "" : "",
            Stato = sapData.TryGetProperty("U_Stato", out var state) ? state.GetString() ?? "" : "",
            TeamTecnico = sapData.TryGetProperty("U_TeamTecnico", out var tech) ? tech.GetString() : null,
            TeamAPL = sapData.TryGetProperty("U_TeamAPL", out var apl) ? apl.GetString() : null,
            Sales = sapData.TryGetProperty("U_Sales", out var sales) ? sales.GetString() : null,
            ProjectManager = sapData.TryGetProperty("U_ProjectManager", out var pm) ? pm.GetString() : null,
            TeamInstallazione = sapData.TryGetProperty("U_TeamInstallazione", out var install) ? install.GetString() : null,
            DataCreazione = sapData.TryGetProperty("U_DataCreazione", out var date) && DateTime.TryParse(date.GetString(), out var dt) ? dt : DateTime.MinValue,
            DataInizioInstallazione = sapData.TryGetProperty("U_DataInizioInstall", out var startDate) && DateTime.TryParse(startDate.GetString(), out var dtStart) ? dtStart : null,
            DataFineInstallazione = sapData.TryGetProperty("U_DataFineInstall", out var endDate) && DateTime.TryParse(endDate.GetString(), out var dtEnd) ? dtEnd : null,
            VersioneWIC = sapData.TryGetProperty("U_VersioneWIC", out var version) ? version.GetString() : null,
            UltimaModifica = sapData.TryGetProperty("U_UltimaModifica", out var lastMod) && DateTime.TryParse(lastMod.GetString(), out var dtMod) ? dtMod : null,
            StatoProgetto = sapData.TryGetProperty("U_StatoProgetto", out var status) ? status.GetString(): null,
            IsInRitardo = sapData.TryGetProperty("U_IsInRitardo", out var delay) && delay.GetString() == "Y",
            Note = sapData.TryGetProperty("U_Note", out var notes) ? notes.GetString() : null,
            ValoreProgetto = sapData.TryGetProperty("U_ValoreProgetto", out var value) ? value.GetDecimal() : null,
            MarginePrevisto = sapData.TryGetProperty("U_MarginePrevisto", out var margin) ? margin.GetDecimal() : null,
            CostiSostenuti = sapData.TryGetProperty("U_CostiSostenuti", out var costs) ? costs.GetDecimal() : 0
        };
    }

    public static object MapLivelloToSap(LivelloProgettoDto dto, string numeroProgetto)
    {
        return new
        {
            Code = $"{numeroProgetto}-L{dto.Id}",
            Name = dto.Nome,
            U_Parent = numeroProgetto,
            U_Ordine = dto.Ordine,
            U_Descrizione = dto.Descrizione ?? "",
            U_DataInizio = dto.DataInizioInstallazione?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "",
            U_DataFine = dto.DataFineInstallazione?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "",
            U_DataCaricamento = dto.DataCaricamento?.ToString("yyyy-MM-ddTHH:mm:ss") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")
        };
    }

    public static object MapProdottoToSap(ProdottoProgettoDto dto, string numeroProgetto)
    {
        return new
        {
            Code = $"{numeroProgetto}-P{dto.Id}",
            Name = $"{dto.TipoProdotto} - {dto.Variante}",
            U_Parent = numeroProgetto,
            U_TipoProdotto = dto.TipoProdotto,
            U_Variante = dto.Variante,
            U_QMq = dto.QMq,
            U_QFt = dto.QFt
        };
    }
}

