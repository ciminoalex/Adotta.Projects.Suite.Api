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
            new { Name = "AX_ADT_PROJECT", Description = "Progetti (UDO)", Type = "bott_MasterData" },
            new { Name = "AX_ADT_PROJLVL", Description = "Livelli Progetto", Type = "bott_MasterDataLines" },
            new { Name = "AX_ADT_PROPRD", Description = "Prodotti Progetto", Type = "bott_MasterDataLines" },
            new { Name = "AX_ADT_PROHIST", Description = "Storico Modifiche", Type = "bott_MasterDataLines" },
            new { Name = "AX_ADT_STATI", Description = "Stati", Type = "bott_MasterData" },
            new { Name = "AX_ADT_CITTA", Description = "Città", Type = "bott_MasterData" },
            new { Name = "AX_ADT_TEAMTECH", Description = "Team Tecnici", Type = "bott_MasterData" },
            new { Name = "AX_ADT_TEAMAPL", Description = "Team APL", Type = "bott_MasterData" },
            new { Name = "AX_ADT_SALES", Description = "Sales", Type = "bott_MasterData" },
            new { Name = "AX_ADT_PMGR", Description = "Project Managers", Type = "bott_MasterData" },
            new { Name = "AX_ADT_SQUADRA", Description = "Squadre Installazione", Type = "bott_MasterData" },
            new { Name = "AX_ADT_PRODMAST", Description = "Prodotti Master", Type = "bott_MasterData" }
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
            ("@AX_ADT_PROJECT", new { Name = "IsInRitardo", Type = "db_Alpha", Size = 1, Description = "In Ritardo (Y/N)", TableName = "@AX_ADT_PROJECT", EditSize = 1, SubType = "st_None" }),

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

            ("@AX_ADT_PROHIST", new { Name = "Parent", Type = "db_Alpha", Size = 30, Description = "Numero Progetto", TableName = "@AX_ADT_PROHIST", EditSize = 30, SubType = "st_None" }),
            ("@AX_ADT_PROHIST", new { Name = "DataModifica", Type = "db_Date", Description = "Data Modifica", TableName = "@AX_ADT_PROHIST" }),
            ("@AX_ADT_PROHIST", new { Name = "UtenteModifica", Type = "db_Alpha", Size = 50, Description = "Utente Modifica", TableName = "@AX_ADT_PROHIST", EditSize = 50, SubType = "st_None" }),
            ("@AX_ADT_PROHIST", new { Name = "CampoModificato", Type = "db_Alpha", Size = 100, Description = "Campo Modificato", TableName = "@AX_ADT_PROHIST", EditSize = 100, SubType = "st_None" }),
            ("@AX_ADT_PROHIST", new { Name = "ValorePrecedente", Type = "db_Memo", Description = "Valore Precedente", TableName = "@AX_ADT_PROHIST" }),
            ("@AX_ADT_PROHIST", new { Name = "NuovoValore", Type = "db_Memo", Description = "Nuovo Valore", TableName = "@AX_ADT_PROHIST" }),
            ("@AX_ADT_PROHIST", new { Name = "VersioneWIC", Type = "db_Alpha", Size = 20, Description = "Versione WIC", TableName = "@AX_ADT_PROHIST", EditSize = 20, SubType = "st_None" })
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
        var exists = await UdoExistsAsync(udoCode, sessionId);
        if (!exists)
        {
            var payload = new
            {
                Code = udoCode,
                Name = "Progetti",
                ObjectType = "boud_MasterData",
                TableName = "AX_ADT_PROJECT",
                CanCreateDefaultForm = "tYES",
                CanCancel = "tNO",
                CanDelete = "tYES",
                CanClose = "tNO",
                ManageSeries = "tNO",
                ChildTables = new[]
                {
                    new { TableName = "AX_ADT_PROJLVL" },
                    new { TableName = "AX_ADT_PROPRD" },
                    new { TableName = "AX_ADT_PROHIST" }
                },
                FindColumns = new[]
                {
                    new { ColumnAlias = "Code", ColumnDescription = "Numero Progetto" },
                    new { ColumnAlias = "Name", ColumnDescription = "Nome Progetto" }
                }
            };

            try
            {
                await _sapClient.CreateRecordAsync<JsonElement>("UserObjectsMD", payload, sessionId);
                steps.Add($"UDO created: {udoCode}");
            }
            catch (Exception ex)
            {
                warnings.Add($"UDO create failed {udoCode}: {ex.Message}");
            }
        }
        else
        {
            steps.Add($"UDO already exists: {udoCode}");
        }

        // Register UDOs for master data tables (no child tables)
        var otherUdos = new[]
        {
            new { Code = "AX_ADT_STATI", Name = "Stati" },
            new { Code = "AX_ADT_CITTA", Name = "Città" },
            new { Code = "AX_ADT_TEAMTECH", Name = "Team Tecnici" },
            new { Code = "AX_ADT_TEAMAPL", Name = "Team APL" },
            new { Code = "AX_ADT_SALES", Name = "Sales" },
            new { Code = "AX_ADT_PMGR", Name = "Project Managers" },
            new { Code = "AX_ADT_SQUADRA", Name = "Squadre Installazione" },
            new { Code = "AX_ADT_PRODMAST", Name = "Prodotti Master" }
        };

        foreach (var u in otherUdos)
        {
            var existsUdo = await UdoExistsAsync(u.Code, sessionId);
            if (!existsUdo)
            {
                var payloadUdo = new
                {
                    Code = u.Code,
                    Name = u.Name,
                    ObjectType = "boud_MasterData",
                    TableName = u.Code,
                    CanCreateDefaultForm = "tYES",
                    CanCancel = "tNO",
                    CanDelete = "tYES",
                    CanClose = "tNO",
                    ManageSeries = "tNO",
                    FindColumns = new[]
                    {
                        new { ColumnAlias = "Code", ColumnDescription = "Codice" },
                        new { ColumnAlias = "Name", ColumnDescription = "Descrizione" }
                    }
                };

                try
                {
                    await _sapClient.CreateRecordAsync<JsonElement>("UserObjectsMD", payloadUdo, sessionId);
                    steps.Add($"UDO created: {u.Code}");
                }
                catch (Exception ex)
                {
                    warnings.Add($"UDO create failed {u.Code}: {ex.Message}");
                }
            }
            else
            {
                steps.Add($"UDO already exists: {u.Code}");
            }
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


