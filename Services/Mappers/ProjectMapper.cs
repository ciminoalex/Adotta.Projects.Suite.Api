using ADOTTA.Projects.Suite.Api.Models;
using ADOTTA.Projects.Suite.Api.DTOs;
using ADOTTA.Projects.Suite.Api.Models.Enums;
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
            U_StatoProgetto = dto.StatoProgetto.ToString(),
            U_ValoreProgetto = dto.ValoreProgetto ?? 0,
            U_MarginePrevisto = dto.MarginePrevisto ?? 0,
            U_CostiSostenuti = dto.CostiSostenuti ?? 0,
            U_Note = dto.Note ?? "",
            U_IsInRitardo = dto.IsInRitardo ? "Y" : "N"
        };
    }

    public static ProjectDto MapSapUDOToProject(JsonElement sapData)
    {
        var project = new ProjectDto
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
            StatoProgetto = sapData.TryGetProperty("U_StatoProgetto", out var status) && Enum.TryParse<ProjectStatus>(status.GetString(), true, out var parsedStatus) ? parsedStatus : ProjectStatus.UPCOMING,
            IsInRitardo = sapData.TryGetProperty("U_IsInRitardo", out var delay) && delay.GetString() == "Y",
            Note = sapData.TryGetProperty("U_Note", out var notes) ? notes.GetString() : null,
            ValoreProgetto = sapData.TryGetProperty("U_ValoreProgetto", out var value) ? value.GetDecimal() : null,
            MarginePrevisto = sapData.TryGetProperty("U_MarginePrevisto", out var margin) ? margin.GetDecimal() : null,
            CostiSostenuti = sapData.TryGetProperty("U_CostiSostenuti", out var costs) ? costs.GetDecimal() : 0
        };

        // Extract child tables from the response (SAP uses Collection suffix)
        if (sapData.TryGetProperty("AX_ADT_PROJLVLCollection", out var livelliArray) && livelliArray.ValueKind == JsonValueKind.Array)
        {
            project.Livelli = new List<LivelloProgettoDto>();
            foreach (var livello in livelliArray.EnumerateArray())
            {
                project.Livelli.Add(MapLivelloFromSap(livello, project.NumeroProgetto));
            }
        }

        if (sapData.TryGetProperty("AX_ADT_PROPRDCollection", out var prodottiArray) && prodottiArray.ValueKind == JsonValueKind.Array)
        {
            project.Prodotti = new List<ProdottoProgettoDto>();
            foreach (var prodotto in prodottiArray.EnumerateArray())
            {
                project.Prodotti.Add(MapProdottoFromSap(prodotto, project.NumeroProgetto));
            }
        }

        return project;
    }

    private static LivelloProgettoDto MapLivelloFromSap(JsonElement sapData, string numeroProgetto)
    {
        var id = 0;
        if (sapData.TryGetProperty("Code", out var code))
        {
            var codeStr = code.GetString();
            if (!string.IsNullOrEmpty(codeStr))
            {
                var parts = codeStr.Split('-');
                if (parts.Length > 1 && parts[1].Length > 1 && int.TryParse(parts[1].Substring(1), out var parsedId))
                {
                    id = parsedId;
                }
            }
        }

        return new LivelloProgettoDto
        {
            Id = id,
            NumeroProgetto = numeroProgetto,
            Nome = sapData.TryGetProperty("Name", out var name) ? name.GetString() ?? "" : "",
            Ordine = sapData.TryGetProperty("U_Ordine", out var order) ? order.GetInt32() : 0,
            Descrizione = sapData.TryGetProperty("U_Descrizione", out var desc) ? desc.GetString() : null,
            DataInizioInstallazione = sapData.TryGetProperty("U_DataInizio", out var start) && DateTime.TryParse(start.GetString(), out var dtStart) ? dtStart : null,
            DataFineInstallazione = sapData.TryGetProperty("U_DataFine", out var end) && DateTime.TryParse(end.GetString(), out var dtEnd) ? dtEnd : null,
            DataCaricamento = sapData.TryGetProperty("U_DataCaricamento", out var load) && DateTime.TryParse(load.GetString(), out var dtLoad) ? dtLoad : null
        };
    }

    private static ProdottoProgettoDto MapProdottoFromSap(JsonElement sapData, string numeroProgetto)
    {
        var id = 0;
        if (sapData.TryGetProperty("Code", out var code))
        {
            var codeStr = code.GetString();
            if (!string.IsNullOrEmpty(codeStr))
            {
                var parts = codeStr.Split('-');
                if (parts.Length > 1 && parts[1].Length > 1 && int.TryParse(parts[1].Substring(1), out var parsedId))
                {
                    id = parsedId;
                }
            }
        }

        return new ProdottoProgettoDto
        {
            Id = id,
            NumeroProgetto = numeroProgetto,
            TipoProdotto = sapData.TryGetProperty("U_TipoProdotto", out var tipo) ? tipo.GetString() ?? "" : "",
            Variante = sapData.TryGetProperty("U_Variante", out var variant) ? variant.GetString() ?? "" : "",
            QMq = sapData.TryGetProperty("U_QMq", out var qmq) ? qmq.GetDecimal() : 0,
            QFt = sapData.TryGetProperty("U_QFt", out var qft) ? qft.GetDecimal() : 0
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

