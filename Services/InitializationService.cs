using System.Text.Json;

namespace ADOTTA.Projects.Suite.Api.Services;

public class InitializationService : IInitializationService
{
    private readonly ISAPServiceLayerClient _sapClient;
    private readonly ILogger<InitializationService> _logger;

    public InitializationService(ISAPServiceLayerClient sapClient, ILogger<InitializationService> logger)
    {
        _sapClient = sapClient;
        _logger = logger;
    }

    public async Task<InitializationResult> InitializeAsync(string sessionId)
    {
        var steps = new List<string>();
        var warnings = new List<string>();

        // 1) UDTs
        await EnsureUserTablesAsync(sessionId, steps, warnings);

        // 2) UDFs
        await EnsureUserFieldsAsync(sessionId, steps, warnings);

        // 3) UDOs
        await EnsureUserObjectsAsync(sessionId, steps, warnings);

        // 4) Custom Queries (none for now)
        steps.Add("Custom queries: none");

        return new InitializationResult { Steps = steps, Warnings = warnings };
    }

    private async Task EnsureUserTablesAsync(string sessionId, List<string> steps, List<string> warnings)
    {
        var tables = new[]
        {
            new { Name = "AX_ADT_PROJECT", Description = "Adt Prjs: Progetti", Type = "bott_MasterData" },
            new { Name = "AX_ADT_PROJLVL", Description = "Adt Prjs: Livelli", Type = "bott_MasterDataLines" },
            new { Name = "AX_ADT_PROPRD", Description = "Adt Prjs: Prodotti", Type = "bott_MasterDataLines" },
            new { Name = "AX_ADT_PROHIST", Description = "Adt Prjs: Storico", Type = "bott_MasterDataLines" },
            new { Name = "AX_ADT_PROMSG", Description = "Adt Prjs: Messaggi", Type = "bott_MasterDataLines" },
            new { Name = "AX_ADT_PROCHG", Description = "Adt Prjs: ChangeLog", Type = "bott_MasterDataLines" },
            new { Name = "AX_ADT_STATI", Description = "Adt Prjs: Stati", Type = "bott_MasterData" },
            new { Name = "AX_ADT_CITTA", Description = "Adt Prjs: Città", Type = "bott_MasterData" },
            new { Name = "AX_ADT_TEAMTECH", Description = "Adt Prjs: Team Tecnici", Type = "bott_MasterData" },
            new { Name = "AX_ADT_TEAMAPL", Description = "Adt Prjs: Team APL", Type = "bott_MasterData" },
            new { Name = "AX_ADT_SALES", Description = "Adt Prjs: Sales", Type = "bott_MasterData" },
            new { Name = "AX_ADT_PMGR", Description = "Adt Prjs: PM", Type = "bott_MasterData" },
            new { Name = "AX_ADT_SQUADRA", Description = "Adt Prjs: Squadre Install", Type = "bott_MasterData" },
            new { Name = "AX_ADT_PRODMAST", Description = "Adt Prjs: Prodotti Master", Type = "bott_MasterData" },
            new { Name = "AX_ADT_TIMESHEET", Description = "Adt Prjs: Timesheet", Type = "bott_Document" },
            new { Name = "AX_ADT_USERS", Description = "Adt Prjs: Users", Type = "bott_MasterData" }
        };

        foreach (var t in tables)
        {
            var exists = await TableExistsAsync(t.Name, sessionId);
            if (exists)
            {
                steps.Add($"UserTable already exists: {t.Name}");
                continue;
            }

            var payload = new
            {
                TableName = t.Name,
                TableDescription = t.Description,
                TableType = t.Type
            };

            try
            {
                await _sapClient.CreateRecordAsync<JsonElement>("UserTablesMD", payload, sessionId);
                steps.Add($"UserTable created: {t.Name}");
            }
            catch (Exception ex)
            {
                warnings.Add($"UserTable create failed {t.Name}: {ex.Message}");
            }
        }
    }

    private async Task EnsureUserFieldsAsync(string sessionId, List<string> steps, List<string> warnings)
    {
        // Define fields for each table (sample based on DTOs/specs)
        var fields = new List<(string Table, object Field)>
        {
            ("@AX_ADT_PROJECT", new { Name = "Cliente", Type = "db_Alpha", Size = 50, Description = "Codice Cliente", TableName = "@AX_ADT_PROJECT", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROJECT", new { Name = "Citta", Type = "db_Alpha", Size = 50, Description = "Città", TableName = "@AX_ADT_PROJECT", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROJECT", new { Name = "Stato", Type = "db_Alpha", Size = 50, Description = "Stato", TableName = "@AX_ADT_PROJECT", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROJECT", new { Name = "TeamTecnico", Type = "db_Alpha", Size = 50, Description = "Team Tecnico", TableName = "@AX_ADT_PROJECT", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROJECT", new { Name = "TeamAPL", Type = "db_Alpha", Size = 50, Description = "Team APL", TableName = "@AX_ADT_PROJECT", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROJECT", new { Name = "Sales", Type = "db_Alpha", Size = 50, Description = "Sales", TableName = "@AX_ADT_PROJECT", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROJECT", new { Name = "ProjectManager", Type = "db_Alpha", Size = 50, Description = "Project Manager", TableName = "@AX_ADT_PROJECT", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROJECT", new { Name = "TeamInstallazione", Type = "db_Alpha", Size = 50, Description = "Team Installazione", TableName = "@AX_ADT_PROJECT", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROJECT", new { Name = "DataCreazione", Type = "db_Date", Description = "Data Creazione", TableName = "@AX_ADT_PROJECT" }),
            ("@AX_ADT_PROJECT", new { Name = "DataInizioInstall", Type = "db_Date", Description = "Data Inizio Installazione", TableName = "@AX_ADT_PROJECT" }),
            ("@AX_ADT_PROJECT", new { Name = "DataFineInstall", Type = "db_Date", Description = "Data Fine Installazione", TableName = "@AX_ADT_PROJECT" }),
            ("@AX_ADT_PROJECT", new { Name = "VersioneWIC", Type = "db_Alpha", Size = 20, Description = "Versione WIC", TableName = "@AX_ADT_PROJECT", EditSize = 20, SubType = "st_None" }),
            ("@AX_ADT_PROJECT", new { Name = "StatoProgetto", Type = "db_Alpha", Size = 50, Description = "Stato Progetto", TableName = "@AX_ADT_PROJECT", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROJECT", new { Name = "ValoreProgetto", Type = "db_Float", Description = "Valore Progetto", TableName = "@AX_ADT_PROJECT", SubType = "st_Sum" }),
            ("@AX_ADT_PROJECT", new { Name = "MarginePrevisto", Type = "db_Float", Description = "Margine Previsto", TableName = "@AX_ADT_PROJECT", SubType = "st_Percentage" }),
            ("@AX_ADT_PROJECT", new { Name = "CostiSostenuti", Type = "db_Float", Description = "Costi Sostenuti", TableName = "@AX_ADT_PROJECT", SubType = "st_Sum" }),
            ("@AX_ADT_PROJECT", new { Name = "QtaTotaleMq", Type = "db_Float", Description = "Quantità totale mq", TableName = "@AX_ADT_PROJECT", SubType = "st_Quantity" }),
            ("@AX_ADT_PROJECT", new { Name = "QtaTotaleFt", Type = "db_Float", Description = "Quantità totale ft", TableName = "@AX_ADT_PROJECT", SubType = "st_Quantity" }),
            ("@AX_ADT_PROJECT", new { Name = "IsInRitardo", Type = "db_Alpha", Size = 1, Description = "In Ritardo (Y/N)", TableName = "@AX_ADT_PROJECT", EditSize = 1, SubType = "st_None" }),
            ("@AX_ADT_PROJECT", new { Name = "Note", Type = "db_Memo", Description = "Note", TableName = "@AX_ADT_PROJECT" }),
            ("@AX_ADT_PROJECT", new { Name = "UltimaModifica", Type = "db_Date", Description = "Ultima Modifica", TableName = "@AX_ADT_PROJECT" }),

            ("@AX_ADT_PROJLVL", new { Name = "Parent", Type = "db_Alpha", Size = 30, Description = "Numero Progetto", TableName = "@AX_ADT_PROJLVL", EditSize = 30, SubType = "st_None" }),
            ("@AX_ADT_PROJLVL", new { Name = "Ordine", Type = "db_Numeric", Description = "Ordine", TableName = "@AX_ADT_PROJLVL" }),
            ("@AX_ADT_PROJLVL", new { Name = "Descrizione", Type = "db_Memo", Description = "Descrizione", TableName = "@AX_ADT_PROJLVL" }),
            ("@AX_ADT_PROJLVL", new { Name = "DataInizio", Type = "db_Date", Description = "Data Inizio Installazione", TableName = "@AX_ADT_PROJLVL" }),
            ("@AX_ADT_PROJLVL", new { Name = "DataFine", Type = "db_Date", Description = "Data Fine Installazione", TableName = "@AX_ADT_PROJLVL" }),
            ("@AX_ADT_PROJLVL", new { Name = "DataCaricamento", Type = "db_Date", Description = "Data Caricamento", TableName = "@AX_ADT_PROJLVL" }),

            ("@AX_ADT_PROPRD", new { Name = "Parent", Type = "db_Alpha", Size = 30, Description = "Numero Progetto", TableName = "@AX_ADT_PROPRD", EditSize = 30, SubType = "st_None" }),
            ("@AX_ADT_PROPRD", new { Name = "TipoProdotto", Type = "db_Alpha", Size = 50, Description = "Tipo Prodotto", TableName = "@AX_ADT_PROPRD", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROPRD", new { Name = "Variante", Type = "db_Alpha", Size = 100, Description = "Variante", TableName = "@AX_ADT_PROPRD", EditSize = 100, SubType = "st_None" }),
            ("@AX_ADT_PROPRD", new { Name = "QMq", Type = "db_Float", Description = "Quantità mq", TableName = "@AX_ADT_PROPRD", SubType = "st_Quantity" }),
            ("@AX_ADT_PROPRD", new { Name = "QFt", Type = "db_Float", Description = "Quantità ft", TableName = "@AX_ADT_PROPRD", SubType = "st_Quantity" }),
            ("@AX_ADT_PROPRD", new { Name = "LivelloId", Type = "db_Alpha", Size = 20, Description = "Livello collegato", TableName = "@AX_ADT_PROPRD", EditSize = 20, SubType = "st_None" }),

            ("@AX_ADT_PROHIST", new { Name = "Parent", Type = "db_Alpha", Size = 30, Description = "Numero Progetto", TableName = "@AX_ADT_PROHIST", EditSize = 30, SubType = "st_None" }),
            ("@AX_ADT_PROHIST", new { Name = "DataModifica", Type = "db_Date", Description = "Data Modifica", TableName = "@AX_ADT_PROHIST" }),
            ("@AX_ADT_PROHIST", new { Name = "UtenteModifica", Type = "db_Alpha", Size = 50, Description = "Utente Modifica", TableName = "@AX_ADT_PROHIST", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROHIST", new { Name = "CampoModificato", Type = "db_Alpha", Size = 100, Description = "Campo Modificato", TableName = "@AX_ADT_PROHIST", EditSize = 100, SubType = "st_None" }),
            ("@AX_ADT_PROHIST", new { Name = "ValorePrecedente", Type = "db_Memo", Description = "Valore Precedente", TableName = "@AX_ADT_PROHIST" }),
            ("@AX_ADT_PROHIST", new { Name = "NuovoValore", Type = "db_Memo", Description = "Nuovo Valore", TableName = "@AX_ADT_PROHIST" }),
            ("@AX_ADT_PROHIST", new { Name = "VersioneWIC", Type = "db_Alpha", Size = 20, Description = "Versione WIC", TableName = "@AX_ADT_PROHIST", EditSize = 20, SubType = "st_None" }),

            ("@AX_ADT_PROMSG", new { Name = "Project", Type = "db_Alpha", Size = 30, Description = "Numero Progetto", TableName = "@AX_ADT_PROMSG", EditSize = 30, SubType = "st_None" }),
            ("@AX_ADT_PROMSG", new { Name = "Data", Type = "db_Date", Description = "Data Messaggio", TableName = "@AX_ADT_PROMSG" }),
            ("@AX_ADT_PROMSG", new { Name = "Utente", Type = "db_Alpha", Size = 50, Description = "Utente", TableName = "@AX_ADT_PROMSG", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROMSG", new { Name = "Messaggio", Type = "db_Memo", Description = "Messaggio", TableName = "@AX_ADT_PROMSG" }),
            ("@AX_ADT_PROMSG", new { Name = "Tipo", Type = "db_Alpha", Size = 20, Description = "Tipo Messaggio", TableName = "@AX_ADT_PROMSG", EditSize = 20, SubType = "st_None" }),
            ("@AX_ADT_PROMSG", new { Name = "Allegato", Type = "db_Memo", Description = "Allegato", TableName = "@AX_ADT_PROMSG" }),

            ("@AX_ADT_PROCHG", new { Name = "Project", Type = "db_Alpha", Size = 30, Description = "Numero Progetto", TableName = "@AX_ADT_PROCHG", EditSize = 30, SubType = "st_None" }),
            ("@AX_ADT_PROCHG", new { Name = "Data", Type = "db_Date", Description = "Data evento", TableName = "@AX_ADT_PROCHG" }),
            ("@AX_ADT_PROCHG", new { Name = "Utente", Type = "db_Alpha", Size = 50, Description = "Utente", TableName = "@AX_ADT_PROCHG", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROCHG", new { Name = "Azione", Type = "db_Alpha", Size = 50, Description = "Azione", TableName = "@AX_ADT_PROCHG", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROCHG", new { Name = "Descrizione", Type = "db_Memo", Description = "Descrizione", TableName = "@AX_ADT_PROCHG" }),
            ("@AX_ADT_PROCHG", new { Name = "DettagliJson", Type = "db_Memo", Description = "Dettagli JSON", TableName = "@AX_ADT_PROCHG" }),

            ("@AX_ADT_TIMESHEET", new { Name = "Progetto", Type = "db_Alpha", Size = 30, Description = "ID Progetto", TableName = "@AX_ADT_TIMESHEET", EditSize = 30, SubType = "st_None" }),
            ("@AX_ADT_TIMESHEET", new { Name = "NumeroProgetto", Type = "db_Alpha", Size = 30, Description = "Numero Progetto", TableName = "@AX_ADT_TIMESHEET", EditSize = 30, SubType = "st_None" }),
            ("@AX_ADT_TIMESHEET", new { Name = "NomeProgetto", Type = "db_Alpha", Size = 100, Description = "Nome Progetto", TableName = "@AX_ADT_TIMESHEET", EditSize = 100, SubType = "st_None" }),
            ("@AX_ADT_TIMESHEET", new { Name = "Cliente", Type = "db_Alpha", Size = 50, Description = "Cliente", TableName = "@AX_ADT_TIMESHEET", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_TIMESHEET", new { Name = "LivelloId", Type = "db_Alpha", Size = 20, Description = "Livello collegato", TableName = "@AX_ADT_TIMESHEET", EditSize = 20, SubType = "st_None" }),
            ("@AX_ADT_TIMESHEET", new { Name = "DataRendicontazione", Type = "db_Date", Description = "Data Rendicontazione", TableName = "@AX_ADT_TIMESHEET" }),
            ("@AX_ADT_TIMESHEET", new { Name = "OreLavorate", Type = "db_Float", Description = "Ore Lavorate", TableName = "@AX_ADT_TIMESHEET", SubType = "st_Quantity" }),
            ("@AX_ADT_TIMESHEET", new { Name = "Note", Type = "db_Memo", Description = "Note", TableName = "@AX_ADT_TIMESHEET" }),
            ("@AX_ADT_TIMESHEET", new { Name = "Utente", Type = "db_Alpha", Size = 50, Description = "Utente", TableName = "@AX_ADT_TIMESHEET", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_TIMESHEET", new { Name = "DataCreazione", Type = "db_Date", Description = "Data Creazione", TableName = "@AX_ADT_TIMESHEET" }),
            ("@AX_ADT_TIMESHEET", new { Name = "UltimaModifica", Type = "db_Date", Description = "Ultima Modifica", TableName = "@AX_ADT_TIMESHEET" }),

            ("@AX_ADT_USERS", new { Name = "Username", Type = "db_Alpha", Size = 50, Description = "Username", TableName = "@AX_ADT_USERS", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_USERS", new { Name = "Email", Type = "db_Alpha", Size = 100, Description = "Email", TableName = "@AX_ADT_USERS", EditSize = 100, SubType = "st_None" }),
            ("@AX_ADT_USERS", new { Name = "Ruolo", Type = "db_Alpha", Size = 30, Description = "Ruolo", TableName = "@AX_ADT_USERS", EditSize = 30, SubType = "st_None" }),
            ("@AX_ADT_USERS", new { Name = "TeamTecnico", Type = "db_Alpha", Size = 50, Description = "Team Tecnico", TableName = "@AX_ADT_USERS", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_USERS", new { Name = "IsActive", Type = "db_Alpha", Size = 1, Description = "Attivo (Y/N)", TableName = "@AX_ADT_USERS", EditSize = 1, SubType = "st_None" }),
            ("@AX_ADT_USERS", new { Name = "Password", Type = "db_Memo", Description = "Password/Hash", TableName = "@AX_ADT_USERS" })
        };

        foreach (var (table, field) in fields)
        {
            var nameProp = field.GetType().GetProperty("Name");
            var name = nameProp?.GetValue(field)?.ToString() ?? string.Empty;
            var exists = await UserFieldExistsAsync(table, name, sessionId);
            if (exists)
            {
                steps.Add($"UserField already exists: {table}.U_{name}");
                continue;
            }

            try
            {
                await _sapClient.CreateRecordAsync<JsonElement>("UserFieldsMD", field, sessionId);
                steps.Add($"UserField created: {table}.U_{name}");
            }
            catch (Exception ex)
            {
                warnings.Add($"UserField create failed {table}.U_{name}: {ex.Message}");
            }
        }
    }

    private async Task EnsureUserObjectsAsync(string sessionId, List<string> steps, List<string> warnings)
    {
        // Only one UDO for projects (master) with child tables navigated independently
        var udoCode = "AX_ADT_PROJECT";
        var projectUdoPayload = new
        {
            Code = udoCode,
            Name = "Adt Prjs: Progetti",
            ObjectType = "boud_MasterData",
            TableName = "AX_ADT_PROJECT",
            CanCreateDefaultForm = "tYES",
            CanCancel = "tNO",
            CanDelete = "tYES",
            CanClose = "tNO",
            ManageSeries = "tNO",
            UserObjectMD_ChildTables = new[]
            {
                new { ObjectName = "AX_ADT_PROJLVL", TableName = "AX_ADT_PROJLVL" },
                new { ObjectName = "AX_ADT_PROPRD", TableName = "AX_ADT_PROPRD" },
                new { ObjectName = "AX_ADT_PROHIST", TableName = "AX_ADT_PROHIST" },
                new { ObjectName = "AX_ADT_PROMSG", TableName = "AX_ADT_PROMSG" },
                new { ObjectName = "AX_ADT_PROCHG", TableName = "AX_ADT_PROCHG" }
            }
        };

        await CreateOrUpdateUdoAsync(udoCode, projectUdoPayload, steps, warnings, sessionId);

        // Register UDOs for master data tables (no child tables)
        var otherUdos = new[]
        {
            new { Code = "AX_ADT_STATI", Name = "Adt Prjs: Stati", ObjectType = (string?)null },
            new { Code = "AX_ADT_CITTA", Name = "Adt Prjs: Città", ObjectType = (string?)null },
            new { Code = "AX_ADT_TEAMTECH", Name = "Adt Prjs: Team Tech", ObjectType = (string?)null },
            new { Code = "AX_ADT_TEAMAPL", Name = "Adt Prjs: Team APL", ObjectType = (string?)null },
            new { Code = "AX_ADT_SALES", Name = "Adt Prjs: Sales", ObjectType = (string?)null },
            new { Code = "AX_ADT_PMGR", Name = "Adt Prjs: PM", ObjectType = (string?)null },
            new { Code = "AX_ADT_SQUADRA", Name = "Adt Prjs: Squadre Install", ObjectType = (string?)null },
            new { Code = "AX_ADT_PRODMAST", Name = "Adt Prjs: Prod. Master", ObjectType = (string?)null },
            new { Code = "AX_ADT_TIMESHEET", Name = "Adt Prjs: Timesheet", ObjectType = (string?)"boud_Document" },
            new { Code = "AX_ADT_PROMSG", Name = "Adt Prjs: Messaggi", ObjectType = (string?)"boud_DocumentLines" },
            new { Code = "AX_ADT_PROCHG", Name = "Adt Prjs: ChangeLog", ObjectType = (string?)"boud_DocumentLines" },
            new { Code = "AX_ADT_USERS", Name = "Adt Prjs: Users", ObjectType = (string?)null }
        };

        foreach (var u in otherUdos)
        {
            var objectType = u.ObjectType ?? "boud_MasterData";
            var payloadUdo = new
            {
                Code = u.Code,
                Name = u.Name,
                ObjectType = objectType,
                TableName = u.Code,
                CanCreateDefaultForm = "tYES",
                CanCancel = "tNO",
                CanDelete = "tYES",
                CanClose = "tNO",
                ManageSeries = "tNO"
            };

            await CreateOrUpdateUdoAsync(u.Code, payloadUdo, steps, warnings, sessionId);
        }
    }

    private async Task<bool> TableExistsAsync(string tableName, string sessionId)
    {
        try
        {
            var record = await _sapClient.GetRecordAsync<JsonElement>("UserTablesMD", tableName, sessionId);
            return record.ValueKind != JsonValueKind.Undefined && record.ValueKind != JsonValueKind.Null;
        }
        catch
        {
            return false;
        }
    }

    private async Task CreateOrUpdateUdoAsync(string code, object payload, List<string> steps, List<string> warnings, string sessionId)
    {
        var exists = await UdoExistsAsync(code, sessionId);
        try
        {
            if (!exists)
            {
                await _sapClient.CreateRecordAsync<JsonElement>("UserObjectsMD", payload, sessionId);
                steps.Add($"UDO created: {code}");
            }
            else
            {
                await _sapClient.UpdateRecordAsync<JsonElement>("UserObjectsMD", code, payload, sessionId);
                steps.Add($"UDO updated: {code}");
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"UDO {(exists ? "update" : "create")} failed {code}: {ex.Message}");
        }
    }

    private async Task<bool> UdoExistsAsync(string code, string sessionId)
    {
        try
        {
            var record = await _sapClient.GetRecordAsync<JsonElement>("UserObjectsMD", code, sessionId);
            return record.ValueKind != JsonValueKind.Undefined && record.ValueKind != JsonValueKind.Null;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> UserFieldExistsAsync(string tableName, string fieldName, string sessionId)
    {
        try
        {
            var records = await _sapClient.GetRecordsAsync<JsonElement>("UserFieldsMD", $"TableName eq '{tableName}' and Name eq '{fieldName}'", sessionId);
            return records.Count > 0;
        }
        catch
        {
            return false;
        }
    }
}


