using ADOTTA.Projects.Suite.Api.Models;
using ADOTTA.Projects.Suite.Api.DTOs;
using ADOTTA.Projects.Suite.Api.Models.Enums;
using System.Text.Json;
using System.Linq;

namespace ADOTTA.Projects.Suite.Api.Services.Mappers;

public static class ProjectMapper
{
    public static object MapProjectToSapUDO(ProjectDto dto, List<ChangeLogDto>? changeLogEntries = null)
    {
        var result = new Dictionary<string, object?>
        {
            ["Code"] = dto.NumeroProgetto,
            ["Name"] = dto.NomeProgetto,
            ["U_Cliente"] = dto.Cliente,
            ["U_Citta"] = dto.Citta,
            ["U_Stato"] = dto.Stato,
            ["U_TeamTecnico"] = dto.TeamTecnico ?? "",
            ["U_TeamAPL"] = dto.TeamAPL ?? "",
            ["U_Sales"] = dto.Sales ?? "",
            ["U_ProjectManager"] = dto.ProjectManager ?? "",
            ["U_TeamInstallazione"] = dto.TeamInstallazione ?? "",
            ["U_DataCreazione"] = dto.DataCreazione.ToString("yyyy-MM-ddTHH:mm:ss"),
            ["U_DataInizioInstall"] = dto.DataInizioInstallazione?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "",
            ["U_DataFineInstall"] = dto.DataFineInstallazione?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "",
            ["U_VersioneWIC"] = dto.VersioneWIC ?? "",
            ["U_UltimaModifica"] = dto.UltimaModifica?.ToString("yyyy-MM-ddTHH:mm:ss") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
            ["U_StatoProgetto"] = dto.StatoProgetto.ToString(),
            ["U_ValoreProgetto"] = dto.ValoreProgetto ?? 0,
            ["U_MarginePrevisto"] = dto.MarginePrevisto ?? 0,
            ["U_CostiSostenuti"] = dto.CostiSostenuti ?? 0,
            ["U_Note"] = dto.Note ?? "",
            ["U_IsInRitardo"] = dto.IsInRitardo ? "Y" : "N",
            ["U_QtaTotaleMq"] = dto.QuantitaTotaleMq ?? 0,
            ["U_QtaTotaleFt"] = dto.QuantitaTotaleFt ?? 0
        };

        // Include child collections for SAP UDO
        // NOTE: For child collections, SAP expects Code to match the parent Code (numero progetto)
        // and uses internal LineId for the row identifier. IDs are mapped from LineId on read.
        if (dto.Livelli != null && dto.Livelli.Count > 0)
        {
            result["AX_ADT_PROJLVLCollection"] = dto.Livelli.Select((l, idx) => new
            {
                Code = dto.NumeroProgetto,
                U_Parent = dto.NumeroProgetto,
                U_Ordine = l.Ordine > 0 ? l.Ordine : idx + 1,
                U_Nome = l.Nome ?? "",
                U_Descrizione = l.Descrizione ?? "",
                // U_LivelloId is the stable logical ID used to link products to levels
                U_LivelloId = l.Id.ToString(),
                U_DataInizio = l.DataInizioInstallazione?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "",
                U_DataFine = l.DataFineInstallazione?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "",
                U_DataCaricamento = l.DataCaricamento?.ToString("yyyy-MM-ddTHH:mm:ss") ?? ""
            }).ToList();
        }

        if (dto.Prodotti != null && dto.Prodotti.Count > 0)
        {
            result["AX_ADT_PROPRDCollection"] = dto.Prodotti.Select((p, idx) => new
            {
                Code = dto.NumeroProgetto,
                U_Parent = dto.NumeroProgetto,
                U_TipoProdotto = p.TipoProdotto,
                U_Variante = p.Variante,
                U_QMq = p.QMq,
                U_QFt = p.QFt,
                U_LivelloId = p.LivelloId?.ToString() ?? ""
            }).ToList();
        }

        // Include ChangeLog collection if provided
        if (changeLogEntries != null && changeLogEntries.Count > 0)
        {
            result["AX_ADT_PROCHGCollection"] = changeLogEntries.Select((chg, idx) => new
            {
                Code = dto.NumeroProgetto,
                U_Project = dto.NumeroProgetto,
                U_Data = chg.Data.ToString("yyyy-MM-ddTHH:mm:ss"),
                U_Utente = chg.Utente,
                U_Azione = chg.Azione,
                U_Descrizione = chg.Descrizione,
                U_DettagliJson = System.Text.Json.JsonSerializer.Serialize(chg.Dettagli ?? new Dictionary<string, string>())
            }).ToList();
        }

        return result;
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
            CostiSostenuti = sapData.TryGetProperty("U_CostiSostenuti", out var costs) ? costs.GetDecimal() : 0,
            QuantitaTotaleMq = sapData.TryGetProperty("U_QtaTotaleMq", out var totMq) ? totMq.GetDecimal() : null,
            QuantitaTotaleFt = sapData.TryGetProperty("U_QtaTotaleFt", out var totFt) ? totFt.GetDecimal() : null
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

        if (sapData.TryGetProperty("AX_ADT_PROHISTCollection", out var storicoArray) && storicoArray.ValueKind == JsonValueKind.Array)
        {
            project.Storico = new List<StoricoModificaDto>();
            foreach (var storico in storicoArray.EnumerateArray())
            {
                project.Storico.Add(MapStoricoFromSap(storico));
            }
        }

        if (sapData.TryGetProperty("AX_ADT_PROMSGCollection", out var messaggiArray) && messaggiArray.ValueKind == JsonValueKind.Array)
        {
            project.Messaggi = new List<MessaggioProgettoDto>();
            foreach (var msg in messaggiArray.EnumerateArray())
            {
                project.Messaggi.Add(MapMessaggioFromSap(msg, project.NumeroProgetto));
            }
        }

        if (sapData.TryGetProperty("AX_ADT_PROCHGCollection", out var changeArray) && changeArray.ValueKind == JsonValueKind.Array)
        {
            project.ChangeLog = new List<ChangeLogDto>();
            foreach (var change in changeArray.EnumerateArray())
            {
                project.ChangeLog.Add(MapChangeLogFromSap(change, project.NumeroProgetto));
            }
        }

        return project;
    }

    public static LivelloProgettoDto MapLivelloFromSap(JsonElement sapData, string numeroProgetto)
    {
        // Prefer the logical U_LivelloId if present, fallback to LineId
        var id = 0;
        if (sapData.TryGetProperty("U_LivelloId", out var uLivelloIdProp) &&
            uLivelloIdProp.ValueKind == JsonValueKind.String &&
            int.TryParse(uLivelloIdProp.GetString(), out var parsedLogicalId))
        {
            id = parsedLogicalId;
        }
        else if (sapData.TryGetProperty("LineId", out var lineIdProp) && lineIdProp.ValueKind == JsonValueKind.Number)
        {
            id = lineIdProp.GetInt32();
        }

        // Try to get nome from U_Nome field first, then from Name, then extract from descrizione for backward compatibility
        var nome = "";
        var descrizione = sapData.TryGetProperty("U_Descrizione", out var desc) ? desc.GetString() : null;
        
        if (sapData.TryGetProperty("U_Nome", out var uNome) && !string.IsNullOrWhiteSpace(uNome.GetString()))
        {
            nome = uNome.GetString() ?? "";
        }
        else if (sapData.TryGetProperty("Name", out var name) && !string.IsNullOrWhiteSpace(name.GetString()))
        {
            nome = name.GetString() ?? "";
        }
        else
        {
            // Backward compatibility: try to extract nome from descrizione if format is "Nome - Descrizione"
            if (!string.IsNullOrEmpty(descrizione) && descrizione.Contains(" - "))
            {
                var parts = descrizione.Split(new[] { " - " }, 2, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    nome = parts[0];
                }
            }
        }

        // If descrizione contains " - " and we extracted nome, remove nome from descrizione
        if (!string.IsNullOrEmpty(descrizione) && !string.IsNullOrEmpty(nome) && descrizione.StartsWith(nome + " - "))
        {
            descrizione = descrizione.Substring(nome.Length + 3);
        }

        return new LivelloProgettoDto
        {
            Id = id,
            NumeroProgetto = numeroProgetto,
            Nome = nome,
            Ordine = sapData.TryGetProperty("U_Ordine", out var order) ? order.GetInt32() : 0,
            Descrizione = descrizione,
            DataInizioInstallazione = sapData.TryGetProperty("U_DataInizio", out var start) && DateTime.TryParse(start.GetString(), out var dtStart) ? dtStart : null,
            DataFineInstallazione = sapData.TryGetProperty("U_DataFine", out var end) && DateTime.TryParse(end.GetString(), out var dtEnd) ? dtEnd : null,
            DataCaricamento = sapData.TryGetProperty("U_DataCaricamento", out var load) && DateTime.TryParse(load.GetString(), out var dtLoad) ? dtLoad : null
        };
    }

    public static ProdottoProgettoDto MapProdottoFromSap(JsonElement sapData, string numeroProgetto)
    {
        // For child collections, SAP uses LineId as row identifier
        var id = sapData.TryGetProperty("LineId", out var lineIdProp) && lineIdProp.ValueKind == JsonValueKind.Number
            ? lineIdProp.GetInt32()
            : 0;

        return new ProdottoProgettoDto
        {
            Id = id,
            NumeroProgetto = numeroProgetto,
            TipoProdotto = sapData.TryGetProperty("U_TipoProdotto", out var tipo) ? tipo.GetString() ?? "" : "",
            Variante = sapData.TryGetProperty("U_Variante", out var variant) ? variant.GetString() ?? "" : "",
            QMq = sapData.TryGetProperty("U_QMq", out var qmq) ? qmq.GetDecimal() : 0,
            QFt = sapData.TryGetProperty("U_QFt", out var qft) ? qft.GetDecimal() : 0,
            LivelloId = sapData.TryGetProperty("U_LivelloId", out var lvlId) && int.TryParse(lvlId.GetString(), out var parsedLvl) ? parsedLvl : null
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
            U_Nome = dto.Nome ?? "",
            U_Descrizione = dto.Descrizione ?? "",
            U_LivelloId = dto.Id.ToString(),
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
            U_QFt = dto.QFt,
            U_LivelloId = dto.LivelloId?.ToString()
        };
    }

    public static StoricoModificaDto MapStoricoFromSap(JsonElement sapData)
    {
        return new StoricoModificaDto
        {
            Id = sapData.TryGetProperty("Code", out var code) ? int.TryParse(code.GetString(), out var parsed) ? parsed : 0 : 0,
            NumeroProgetto = sapData.TryGetProperty("U_Parent", out var parent) ? parent.GetString() ?? string.Empty : string.Empty,
            DataModifica = sapData.TryGetProperty("U_DataModifica", out var date) && DateTime.TryParse(date.GetString(), out var dt) ? dt : DateTime.MinValue,
            UtenteModifica = sapData.TryGetProperty("U_UtenteModifica", out var user) ? user.GetString() ?? string.Empty : string.Empty,
            CampoModificato = sapData.TryGetProperty("U_CampoModificato", out var field) ? field.GetString() ?? string.Empty : string.Empty,
            ValorePrecedente = sapData.TryGetProperty("U_ValorePrecedente", out var oldVal) ? oldVal.GetString() : null,
            NuovoValore = sapData.TryGetProperty("U_NuovoValore", out var newVal) ? newVal.GetString() : null,
            VersioneWIC = sapData.TryGetProperty("U_VersioneWIC", out var version) ? version.GetString() : null,
            Descrizione = sapData.TryGetProperty("U_Descrizione", out var desc) ? desc.GetString() : null
        };
    }

    public static MessaggioProgettoDto MapMessaggioFromSap(JsonElement sapData, string numeroProgetto)
    {
        // Prefer SAP system fields CreateDate + CreateTime for the message timestamp.
        // Fallback to U_Data (legacy/custom field) and finally to UtcNow if nothing is available.
        DateTime dataMessaggio = DateTime.UtcNow;

        try
        {
            if (sapData.TryGetProperty("CreateDate", out var createDateProp) &&
                sapData.TryGetProperty("CreateTime", out var createTimeProp))
            {
                // Typical SAP format: dates as "yyyyMMdd" or "20241129", times as "HHmmss" or numeric seconds.
                var createDateStr = createDateProp.GetString();
                var createTimeStr = createTimeProp.GetString();

                if (!string.IsNullOrWhiteSpace(createDateStr) && !string.IsNullOrWhiteSpace(createTimeStr))
                {
                    // Normalize to fixed-length strings if they come as numbers
                    if (int.TryParse(createDateStr, out var createDateInt))
                    {
                        createDateStr = createDateInt.ToString("00000000");
                    }
                    if (int.TryParse(createTimeStr, out var createTimeInt))
                    {
                        createTimeStr = createTimeInt.ToString("000000");
                    }

                    if (createDateStr.Length == 8 && createTimeStr.Length == 6)
                    {
                        var year = int.Parse(createDateStr[..4]);
                        var month = int.Parse(createDateStr.Substring(4, 2));
                        var day = int.Parse(createDateStr.Substring(6, 2));

                        var hour = int.Parse(createTimeStr[..2]);
                        var minute = int.Parse(createTimeStr.Substring(2, 2));
                        var second = int.Parse(createTimeStr.Substring(4, 2));

                        dataMessaggio = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
                    }
                }
            }
            else if (sapData.TryGetProperty("U_Data", out var legacyDateProp) &&
                     DateTime.TryParse(legacyDateProp.GetString(), out var legacyDt))
            {
                dataMessaggio = legacyDt;
            }
        }
        catch
        {
            // In caso di problemi di parsing, manteniamo il fallback a UtcNow
            dataMessaggio = DateTime.UtcNow;
        }

        return new MessaggioProgettoDto
        {
            Id = sapData.TryGetProperty("Code", out var code) ? code.GetString() : "",
            NumeroProgetto = numeroProgetto,
            Data = dataMessaggio,
            Utente = sapData.TryGetProperty("U_Utente", out var user) ? user.GetString() ?? string.Empty : string.Empty,
            Messaggio = sapData.TryGetProperty("U_Messaggio", out var msg) ? msg.GetString() ?? string.Empty : string.Empty,
            Tipo = sapData.TryGetProperty("U_Tipo", out var tipo) ? tipo.GetString() ?? "info" : "info",
            Allegato = sapData.TryGetProperty("U_Allegato", out var attachment) ? attachment.GetString() : null
        };
    }

    public static ChangeLogDto MapChangeLogFromSap(JsonElement sapData, string numeroProgetto)
    {
        var dettagliDict = default(Dictionary<string, string>?);
        if (sapData.TryGetProperty("U_DettagliJson", out var dett) && dett.ValueKind == JsonValueKind.String)
        {
            try
            {
                dettagliDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(dett.GetString() ?? "{}");
            }
            catch
            {
                dettagliDict = null;
            }
        }

        return new ChangeLogDto
        {
            Id = sapData.TryGetProperty("Code", out var code) ? ExtractNumericId(code.GetString()) : 0,
            NumeroProgetto = numeroProgetto,
            Data = sapData.TryGetProperty("U_Data", out var date) && DateTime.TryParse(date.GetString(), out var dt) ? dt : DateTime.UtcNow,
            Utente = sapData.TryGetProperty("U_Utente", out var user) ? user.GetString() ?? string.Empty : string.Empty,
            Azione = sapData.TryGetProperty("U_Azione", out var action) ? action.GetString() ?? string.Empty : string.Empty,
            Descrizione = sapData.TryGetProperty("U_Descrizione", out var desc) ? desc.GetString() ?? string.Empty : string.Empty,
            Dettagli = dettagliDict
        };
    }

    private static int ExtractNumericId(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return 0;
        var digits = new string(code.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, out var parsed) ? parsed : 0;
    }
}

