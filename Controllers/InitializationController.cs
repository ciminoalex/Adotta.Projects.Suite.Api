using ADOTTA.Projects.Suite.Api.Configuration;
using ADOTTA.Projects.Suite.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ADOTTA.Projects.Suite.Api.Controllers;

[ApiController]
[Route("api/init")]
public class InitializationController : ControllerBase
{
    private readonly IInitializationService _initService;
    private readonly ILogger<InitializationController> _logger;
    private readonly ISAPServiceLayerClient _sapClient;
    private readonly SAPSettings _sapSettings;

    public InitializationController(
        IInitializationService initService,
        ILogger<InitializationController> logger,
        ISAPServiceLayerClient sapClient,
        IOptions<SAPSettings> sapOptions)
    {
        _initService = initService;
        _logger = logger;
        _sapClient = sapClient;
        _sapSettings = sapOptions.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Initialize()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_sapSettings.CompanyDB) ||
                string.IsNullOrWhiteSpace(_sapSettings.UserName) ||
                string.IsNullOrWhiteSpace(_sapSettings.Password))
            {
                return StatusCode(500, new { message = "Configurazione SAP incompleta in appsettings." });
            }

            var loginResponse = await _sapClient.LoginAsync(new LoginRequest
            {
                CompanyDB = _sapSettings.CompanyDB,
                UserName = _sapSettings.UserName,
                Password = _sapSettings.Password
            });

            if (string.IsNullOrWhiteSpace(loginResponse.SessionId))
            {
                return StatusCode(500, new { message = "Impossibile ottenere una sessione SAP valida." });
            }

            var result = await _initService.InitializeAsync(loginResponse.SessionId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialization failed");
            return StatusCode(500, new { message = "Initialization failed", error = ex.Message });
        }
    }

    [HttpPost("seed-stati")]
    public async Task<IActionResult> SeedStati()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_sapSettings.CompanyDB) ||
                string.IsNullOrWhiteSpace(_sapSettings.UserName) ||
                string.IsNullOrWhiteSpace(_sapSettings.Password))
            {
                return StatusCode(500, new { message = "Configurazione SAP incompleta in appsettings." });
            }

            var loginResponse = await _sapClient.LoginAsync(new LoginRequest
            {
                CompanyDB = _sapSettings.CompanyDB,
                UserName = _sapSettings.UserName,
                Password = _sapSettings.Password
            });

            if (string.IsNullOrWhiteSpace(loginResponse.SessionId))
            {
                return StatusCode(500, new { message = "Impossibile ottenere una sessione SAP valida." });
            }

            var stati = GetStatiEuropeiEUSA();
            var created = 0;
            var skipped = 0;
            var errors = new List<string>();

            foreach (var stato in stati)
            {
                try
                {
                    // Verifica se lo stato esiste già
                    System.Text.Json.JsonElement? existing = null;
                    bool exists = false;
                    try
                    {
                        existing = await _sapClient.GetRecordAsync<System.Text.Json.JsonElement>("AX_ADT_STATI", stato.Code, loginResponse.SessionId);
                        exists = existing.Value.ValueKind != System.Text.Json.JsonValueKind.Undefined && existing.Value.ValueKind != System.Text.Json.JsonValueKind.Null;
                    }
                    catch
                    {
                        // Lo stato non esiste
                        exists = false;
                    }

                    var payload = new
                    {
                        Code = stato.Code,
                        Name = stato.Name,
                        U_CodiceISO = stato.CodiceISO,
                        U_Continente = stato.Continente
                    };

                    if (exists && existing.HasValue)
                    {
                        // Verifica se i campi custom sono vuoti o mancanti
                        var hasCodiceISO = existing.Value.TryGetProperty("U_CodiceISO", out var codiceISO) && 
                                         codiceISO.ValueKind != System.Text.Json.JsonValueKind.Null && 
                                         !string.IsNullOrWhiteSpace(codiceISO.GetString());
                        var hasContinente = existing.Value.TryGetProperty("U_Continente", out var continente) && 
                                          continente.ValueKind != System.Text.Json.JsonValueKind.Null && 
                                          !string.IsNullOrWhiteSpace(continente.GetString());

                        if (!hasCodiceISO || !hasContinente)
                        {
                            // Aggiorna il record esistente con i campi mancanti
                            await _sapClient.UpdateRecordAsync<System.Text.Json.JsonElement>("AX_ADT_STATI", stato.Code, payload, loginResponse.SessionId);
                            created++;
                            _logger.LogInformation("Stato aggiornato: {Code} - {Name}", stato.Code, stato.Name);
                        }
                        else
                        {
                            skipped++;
                            _logger.LogInformation("Stato già esistente e completo: {Code} - {Name}", stato.Code, stato.Name);
                        }
                    }
                    else
                    {
                        // Crea il nuovo record
                        await _sapClient.CreateRecordAsync<System.Text.Json.JsonElement>("AX_ADT_STATI", payload, loginResponse.SessionId);
                        created++;
                        _logger.LogInformation("Stato creato: {Code} - {Name}", stato.Code, stato.Name);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{stato.Code} ({stato.Name}): {ex.Message}");
                    _logger.LogError(ex, "Errore nella creazione/aggiornamento dello stato {Code}", stato.Code);
                }
            }

            await _sapClient.LogoutAsync(loginResponse.SessionId);

            return Ok(new
            {
                message = "Seed stati completato",
                created,
                skipped,
                total = stati.Count,
                errors = errors.Any() ? errors : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seed stati failed");
            return StatusCode(500, new { message = "Seed stati failed", error = ex.Message });
        }
    }

    private List<(string Code, string Name, string CodiceISO, string Continente)> GetStatiEuropeiEUSA()
    {
        var stati = new List<(string Code, string Name, string CodiceISO, string Continente)>();

        // Stati Europei (27 membri UE + altri stati europei)
        var statiEuropei = new[]
        {
            ("IT", "Italia", "ITA", "Europa"),
            ("DE", "Germania", "DEU", "Europa"),
            ("FR", "Francia", "FRA", "Europa"),
            ("ES", "Spagna", "ESP", "Europa"),
            ("PL", "Polonia", "POL", "Europa"),
            ("RO", "Romania", "ROU", "Europa"),
            ("NL", "Paesi Bassi", "NLD", "Europa"),
            ("BE", "Belgio", "BEL", "Europa"),
            ("GR", "Grecia", "GRC", "Europa"),
            ("PT", "Portogallo", "PRT", "Europa"),
            ("CZ", "Repubblica Ceca", "CZE", "Europa"),
            ("HU", "Ungheria", "HUN", "Europa"),
            ("SE", "Svezia", "SWE", "Europa"),
            ("AT", "Austria", "AUT", "Europa"),
            ("BG", "Bulgaria", "BGR", "Europa"),
            ("DK", "Danimarca", "DNK", "Europa"),
            ("FI", "Finlandia", "FIN", "Europa"),
            ("SK", "Slovacchia", "SVK", "Europa"),
            ("IE", "Irlanda", "IRL", "Europa"),
            ("HR", "Croazia", "HRV", "Europa"),
            ("LT", "Lituania", "LTU", "Europa"),
            ("SI", "Slovenia", "SVN", "Europa"),
            ("LV", "Lettonia", "LVA", "Europa"),
            ("EE", "Estonia", "EST", "Europa"),
            ("CY", "Cipro", "CYP", "Europa"),
            ("LU", "Lussemburgo", "LUX", "Europa"),
            ("MT", "Malta", "MLT", "Europa"),
            ("GB", "Regno Unito", "GBR", "Europa"),
            ("CH", "Svizzera", "CHE", "Europa"),
            ("NO", "Norvegia", "NOR", "Europa"),
            ("IS", "Islanda", "ISL", "Europa"),
            ("AL", "Albania", "ALB", "Europa"),
            ("BA", "Bosnia ed Erzegovina", "BIH", "Europa"),
            ("MK", "Macedonia del Nord", "MKD", "Europa"),
            ("RS", "Serbia", "SRB", "Europa"),
            ("ME", "Montenegro", "MNE", "Europa"),
            ("XK", "Kosovo", "XKX", "Europa"),
            ("UA", "Ucraina", "UKR", "Europa"),
            ("BY", "Bielorussia", "BLR", "Europa"),
            ("MD", "Moldavia", "MDA", "Europa"),
            ("TR", "Turchia", "TUR", "Europa"),
            ("RU", "Russia", "RUS", "Europa")
        };

        stati.AddRange(statiEuropei);

        // Stati Uniti d'America (50 stati)
        var statiUSA = new[]
        {
            ("US-AL", "Alabama", "USA", "Nord America"),
            ("US-AK", "Alaska", "USA", "Nord America"),
            ("US-AZ", "Arizona", "USA", "Nord America"),
            ("US-AR", "Arkansas", "USA", "Nord America"),
            ("US-CA", "California", "USA", "Nord America"),
            ("US-CO", "Colorado", "USA", "Nord America"),
            ("US-CT", "Connecticut", "USA", "Nord America"),
            ("US-DE", "Delaware", "USA", "Nord America"),
            ("US-FL", "Florida", "USA", "Nord America"),
            ("US-GA", "Georgia", "USA", "Nord America"),
            ("US-HI", "Hawaii", "USA", "Nord America"),
            ("US-ID", "Idaho", "USA", "Nord America"),
            ("US-IL", "Illinois", "USA", "Nord America"),
            ("US-IN", "Indiana", "USA", "Nord America"),
            ("US-IA", "Iowa", "USA", "Nord America"),
            ("US-KS", "Kansas", "USA", "Nord America"),
            ("US-KY", "Kentucky", "USA", "Nord America"),
            ("US-LA", "Louisiana", "USA", "Nord America"),
            ("US-ME", "Maine", "USA", "Nord America"),
            ("US-MD", "Maryland", "USA", "Nord America"),
            ("US-MA", "Massachusetts", "USA", "Nord America"),
            ("US-MI", "Michigan", "USA", "Nord America"),
            ("US-MN", "Minnesota", "USA", "Nord America"),
            ("US-MS", "Mississippi", "USA", "Nord America"),
            ("US-MO", "Missouri", "USA", "Nord America"),
            ("US-MT", "Montana", "USA", "Nord America"),
            ("US-NE", "Nebraska", "USA", "Nord America"),
            ("US-NV", "Nevada", "USA", "Nord America"),
            ("US-NH", "New Hampshire", "USA", "Nord America"),
            ("US-NJ", "New Jersey", "USA", "Nord America"),
            ("US-NM", "New Mexico", "USA", "Nord America"),
            ("US-NY", "New York", "USA", "Nord America"),
            ("US-NC", "North Carolina", "USA", "Nord America"),
            ("US-ND", "North Dakota", "USA", "Nord America"),
            ("US-OH", "Ohio", "USA", "Nord America"),
            ("US-OK", "Oklahoma", "USA", "Nord America"),
            ("US-OR", "Oregon", "USA", "Nord America"),
            ("US-PA", "Pennsylvania", "USA", "Nord America"),
            ("US-RI", "Rhode Island", "USA", "Nord America"),
            ("US-SC", "South Carolina", "USA", "Nord America"),
            ("US-SD", "South Dakota", "USA", "Nord America"),
            ("US-TN", "Tennessee", "USA", "Nord America"),
            ("US-TX", "Texas", "USA", "Nord America"),
            ("US-UT", "Utah", "USA", "Nord America"),
            ("US-VT", "Vermont", "USA", "Nord America"),
            ("US-VA", "Virginia", "USA", "Nord America"),
            ("US-WA", "Washington", "USA", "Nord America"),
            ("US-WV", "West Virginia", "USA", "Nord America"),
            ("US-WI", "Wisconsin", "USA", "Nord America"),
            ("US-WY", "Wyoming", "USA", "Nord America"),
            ("US-DC", "District of Columbia", "USA", "Nord America")
        };

        stati.AddRange(statiUSA);

            return stati;
    }

    [HttpPost("debug-stati")]
    public async Task<IActionResult> DebugStati()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_sapSettings.CompanyDB) ||
                string.IsNullOrWhiteSpace(_sapSettings.UserName) ||
                string.IsNullOrWhiteSpace(_sapSettings.Password))
            {
                return StatusCode(500, new { message = "Configurazione SAP incompleta in appsettings." });
            }

            var loginResponse = await _sapClient.LoginAsync(new LoginRequest
            {
                CompanyDB = _sapSettings.CompanyDB,
                UserName = _sapSettings.UserName,
                Password = _sapSettings.Password
            });

            if (string.IsNullOrWhiteSpace(loginResponse.SessionId))
            {
                return StatusCode(500, new { message = "Impossibile ottenere una sessione SAP valida." });
            }

            // Prova a recuperare direttamente dal Service Layer con $top
            var rawData = await _sapClient.GetRecordsAsync<System.Text.Json.JsonElement>("AX_ADT_STATI", null, loginResponse.SessionId);
            
            // Prova anche con il prefisso @
            var rawDataWithAt = new List<System.Text.Json.JsonElement>();
            try
            {
                rawDataWithAt = await _sapClient.GetRecordsAsync<System.Text.Json.JsonElement>("@AX_ADT_STATI", null, loginResponse.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tentativo con @AX_ADT_STATI fallito");
            }
            
            // Prova a recuperare la risposta raw per vedere se c'è un link next
            var rawResponse = await GetRawResponseAsync("AX_ADT_STATI", null, loginResponse.SessionId);

            // Prova a recuperare un singolo record
            var singleRecord = new System.Text.Json.JsonElement();
            try
            {
                singleRecord = await _sapClient.GetRecordAsync<System.Text.Json.JsonElement>("AX_ADT_STATI", "IT", loginResponse.SessionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tentativo di recupero singolo record fallito: {Error}", ex.Message);
            }

            await _sapClient.LogoutAsync(loginResponse.SessionId);

            return Ok(new
            {
                countWithoutAt = rawData.Count,
                countWithAt = rawDataWithAt.Count,
                sampleWithoutAt = rawData.Take(3).Select(r => r.ToString()).ToList(),
                sampleWithAt = rawDataWithAt.Take(3).Select(r => r.ToString()).ToList(),
                singleRecord = singleRecord.ValueKind != System.Text.Json.JsonValueKind.Undefined ? singleRecord.ToString() : "Not found",
                rawResponseMetadata = rawResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Debug stati failed");
            return StatusCode(500, new { message = "Debug stati failed", error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    private async Task<object> GetRawResponseAsync(string tableName, string? filter, string sessionId)
    {
        try
        {
            var url = tableName;
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(filter))
            {
                queryParams.Add($"$filter={Uri.EscapeDataString(filter)}");
            }
            
            queryParams.Add("$top=100");
            queryParams.Add("$skip=0");
            
            url += "?" + string.Join("&", queryParams);

            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_sapSettings.ServiceLayerUrl?.Trim() ?? string.Empty);
            httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var cookieHeader = $"B1SESSION={sessionId}";
            request.Headers.Add("Cookie", cookieHeader);

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var jsonDoc = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonDocument>();
            if (jsonDoc != null)
            {
                var metadata = new Dictionary<string, object>();
                
                // Cerca tutti i possibili link di paginazione
                foreach (var prop in jsonDoc.RootElement.EnumerateObject())
                {
                    if (prop.Name.Contains("next", StringComparison.OrdinalIgnoreCase) || 
                        prop.Name.Contains("link", StringComparison.OrdinalIgnoreCase) ||
                        prop.Name.Contains("odata", StringComparison.OrdinalIgnoreCase))
                    {
                        metadata[prop.Name] = prop.Value.ToString();
                    }
                }
                
                // Conta i record
                int recordCount = 0;
                if (jsonDoc.RootElement.TryGetProperty("value", out var valueArray))
                {
                    recordCount = valueArray.GetArrayLength();
                }
                else if (jsonDoc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    recordCount = jsonDoc.RootElement.GetArrayLength();
                }
                
                metadata["recordCount"] = recordCount;
                metadata["rootElementType"] = jsonDoc.RootElement.ValueKind.ToString();
                
                return metadata;
            }
            
            return new { error = "No JSON document" };
        }
        catch (Exception ex)
        {
            return new { error = ex.Message };
        }
    }

    [HttpPost("debug-clienti")]
    public async Task<IActionResult> DebugClienti()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_sapSettings.CompanyDB) ||
                string.IsNullOrWhiteSpace(_sapSettings.UserName) ||
                string.IsNullOrWhiteSpace(_sapSettings.Password))
            {
                return StatusCode(500, new { message = "Configurazione SAP incompleta in appsettings." });
            }

            var loginResponse = await _sapClient.LoginAsync(new LoginRequest
            {
                CompanyDB = _sapSettings.CompanyDB,
                UserName = _sapSettings.UserName,
                Password = _sapSettings.Password
            });

            if (string.IsNullOrWhiteSpace(loginResponse.SessionId))
            {
                return StatusCode(500, new { message = "Impossibile ottenere una sessione SAP valida." });
            }

            // Prova a recuperare direttamente dal Service Layer
            var rawData = await _sapClient.GetRecordsAsync<System.Text.Json.JsonElement>("BusinessPartners", "CardType eq 'C'", loginResponse.SessionId);

            await _sapClient.LogoutAsync(loginResponse.SessionId);

            return Ok(new
            {
                count = rawData.Count,
                sample = rawData.Take(3).Select(r => new
                {
                    CardCode = r.TryGetProperty("CardCode", out var code) ? code.GetString() : null,
                    CardName = r.TryGetProperty("CardName", out var name) ? name.GetString() : null
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Debug clienti failed");
            return StatusCode(500, new { message = "Debug clienti failed", error = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}


