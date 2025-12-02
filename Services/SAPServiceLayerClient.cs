using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ADOTTA.Projects.Suite.Api.Configuration;
using Microsoft.Extensions.Options;

namespace ADOTTA.Projects.Suite.Api.Services;

public class SAPServiceLayerClient : ISAPServiceLayerClient
{
    private readonly HttpClient _httpClient;
    private readonly SAPSettings _settings;
    private readonly ILogger<SAPServiceLayerClient> _logger;
    private string? _routeId;

    public SAPServiceLayerClient(HttpClient httpClient, IOptions<SAPSettings> settings, ILogger<SAPServiceLayerClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        
        var baseUrl = _settings.ServiceLayerUrl?.Trim() ?? string.Empty;
        if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
        {
            baseUrl += "/";
        }
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestVersion = System.Net.HttpVersion.Version11;
        _httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        _httpClient.Timeout = TimeSpan.FromSeconds(100);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private void ExtractRouteIdFromResponse(HttpResponseMessage response)
    {
        try
        {
            // Try to extract ROUTEID from Set-Cookie header
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
            {
                foreach (var setCookie in setCookieHeaders)
                {
                    if (setCookie.Contains("ROUTEID="))
                    {
                        var routeIdPart = setCookie.Split(';').FirstOrDefault(p => p.Trim().StartsWith("ROUTEID="));
                        if (routeIdPart != null)
                        {
                            var routeIdValue = routeIdPart.Split('=')[1];
                            _routeId = routeIdValue.Trim();
                            _logger.LogDebug("Extracted ROUTEID from response: {RouteId}", _routeId);
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not extract ROUTEID from response headers");
        }
    }

    private string BuildCookieHeader(string sessionId)
    {
        // Add B1SESSION to CookieContainer instead of header
        try
        {
            if (_httpClient.BaseAddress != null)
            {
                var handler = GetHandlerFromHttpClient(_httpClient);
                
                if (handler is System.Net.Http.HttpClientHandler httpHandler && httpHandler.CookieContainer != null)
                {
                    // Add B1SESSION cookie to the container
                    var b1SessionCookie = new System.Net.Cookie("B1SESSION", sessionId)
                    {
                        Domain = _httpClient.BaseAddress.Host,
                        Path = "/"
                    };
                    httpHandler.CookieContainer.Add(_httpClient.BaseAddress, b1SessionCookie);
                    _logger.LogDebug("Added B1SESSION to CookieContainer");
                    
                    // Let CookieContainer handle cookies automatically - don't add manual header
                    return string.Empty;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not add B1SESSION to CookieContainer");
        }
        
        // Fallback: return manual cookie header if CookieContainer not available
        var cookies = new List<string> { $"B1SESSION={sessionId}" };
        
        // Try to get ROUTEID from CookieContainer
        try
        {
            if (_httpClient.BaseAddress != null)
            {
                var handler = GetHandlerFromHttpClient(_httpClient);
                
                if (handler is System.Net.Http.HttpClientHandler httpHandler && httpHandler.CookieContainer != null)
                {
                    var cookieCollection = httpHandler.CookieContainer.GetCookies(_httpClient.BaseAddress);
                    
                    foreach (System.Net.Cookie cookie in cookieCollection)
                    {
                        if (cookie.Name == "ROUTEID")
                        {
                            cookies.Add($"ROUTEID={cookie.Value}");
                            _logger.LogDebug("Found ROUTEID in CookieContainer: {Value}", cookie.Value);
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not extract ROUTEID from CookieContainer");
        }
        
        var cookieHeader = string.Join("; ", cookies);
        _logger.LogDebug("Fallback cookie header: {CookieHeader}", cookieHeader);
        return cookieHeader;
    }

    private object? GetHandlerFromHttpClient(HttpClient httpClient)
    {
        // Try different field names that might be used in HttpClient
        var possibleFieldNames = new[] { "_handler", "_primaryHandler", "handler" };
        
        foreach (var fieldName in possibleFieldNames)
        {
            try
            {
                var field = typeof(HttpClient).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var handler = field.GetValue(httpClient);
                    if (handler != null)
                    {
                        return handler;
                    }
                }
            }
            catch
            {
                // Continue to next field name
            }
        }
        
        return null;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            };

            // Log JSON payload for Login request
            var loginJson = JsonSerializer.Serialize(request, serializeOptions);
            _logger.LogDebug("SAP Login request payload: {Payload}", loginJson);
            // Preflight to get ROUTEID/sticky cookie from the load balancer
            try
            {
                using var preflight = await _httpClient.GetAsync(string.Empty);
                // Extract ROUTEID from Set-Cookie header if present
                ExtractRouteIdFromResponse(preflight);
            }
            catch (Exception pfEx)
            {
                _logger.LogDebug(pfEx, "Preflight request before SAP Login failed; continuing to Login");
            }

            var response = await _httpClient.PostAsJsonAsync("Login", request, serializeOptions);
            
            // Extract ROUTEID from Login response as well
            ExtractRouteIdFromResponse(response);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result != null)
                {
                    _logger.LogInformation("SAP Login successful. SessionId: {SessionId}, ROUTEID: {RouteId}", result.SessionId, _routeId ?? "none");
                    return result;
                }
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("SAP Login failed. Status: {Status}, Content: {Content}", response.StatusCode, errorContent);
            throw new Exception($"SAP Login failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SAP Login");
            throw;
        }
    }

    public async Task LogoutAsync(string sessionId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "Logout");
            var cookieHeader = BuildCookieHeader(sessionId);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.Add("Cookie", cookieHeader);
            }
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("SAP Logout successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SAP Logout");
            throw;
        }
    }

    public async Task<List<T>> GetRecordsAsync<T>(string tableName, string? filter = null, string sessionId = "")
    {
        try
        {
            var allRecords = new List<T>();
            var skip = 0;
            const int maxPageSize = 50000; // Dimensione massima della pagina usando l'header Prefer
            var hasMore = true;

            while (hasMore)
            {
                var url = tableName;
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(filter))
                {
                    queryParams.Add($"$filter={Uri.EscapeDataString(filter)}");
                }
                
                // Usa $skip per la paginazione
                queryParams.Add($"$skip={skip}");
                
                if (queryParams.Any())
                {
                    url += "?" + string.Join("&", queryParams);
                }

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                
                // Aggiungi l'header Prefer per impostare il maxpagesize
                request.Headers.Add("Prefer", "odata.maxpagesize=50000");
                
                if (!string.IsNullOrEmpty(sessionId))
                {
                    var cookieHeader = BuildCookieHeader(sessionId);
                    if (!string.IsNullOrEmpty(cookieHeader))
                    {
                        request.Headers.Add("Cookie", cookieHeader);
                    }
                }

                var fullUrl = _httpClient.BaseAddress != null ? new Uri(_httpClient.BaseAddress, url).ToString() : url;
                _logger.LogDebug("SAP GET relative URL: {RelativeUrl}, full URL: {FullUrl}, skip: {Skip}, maxpagesize: {MaxPageSize}", url, fullUrl, skip, maxPageSize);
                var response = await _httpClient.SendAsync(request);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    break;
                }
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("SAP GET failed. Relative URL: {RelativeUrl}, Full URL: {FullUrl}, Status: {Status}, Body: {Body}", url, fullUrl, response.StatusCode, errorBody);
                    var ex = new HttpRequestException($"SAP GET {url} failed. Status: {response.StatusCode}. Body: {errorBody}");
                    ex.Data["StatusCode"] = response.StatusCode;
                    throw ex;
                }

                var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
                if (jsonDoc != null)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    List<T>? batchRecords = null;
                    
                    // Check if it's OData format with "value" property
                    if (jsonDoc.RootElement.TryGetProperty("value", out var valueArray))
                    {
                        batchRecords = JsonSerializer.Deserialize<List<T>>(valueArray.GetRawText(), options);
                        
                        // Verifica se c'è un link "next" per la paginazione OData
                        if (jsonDoc.RootElement.TryGetProperty("odata.nextLink", out var nextLink) || 
                            jsonDoc.RootElement.TryGetProperty("@odata.nextLink", out nextLink))
                        {
                            // C'è un link next, quindi ci sono altri record
                            hasMore = batchRecords != null && batchRecords.Count > 0;
                        }
                        else
                        {
                            // Nessun link next, verifica se abbiamo ricevuto esattamente maxPageSize record
                            hasMore = batchRecords != null && batchRecords.Count == maxPageSize;
                        }
                    }
                    // Check if it's a direct array (UDO format)
                    else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        batchRecords = JsonSerializer.Deserialize<List<T>>(jsonDoc.RootElement.GetRawText(), options);
                        // Per gli array diretti, verifica se abbiamo ricevuto esattamente maxPageSize record
                        hasMore = batchRecords != null && batchRecords.Count == maxPageSize;
                    }
                    
                    if (batchRecords != null && batchRecords.Count > 0)
                    {
                        allRecords.AddRange(batchRecords);
                        _logger.LogDebug("Recuperati {Count} record, totale accumulato: {Total}", batchRecords.Count, allRecords.Count);
                        
                        // Se riceviamo sempre 20 record (limite default del Service Layer), continuiamo a paginare
                        // fino a quando non riceviamo meno di 20 record o non ci sono più record nuovi
                        var previousCount = allRecords.Count - batchRecords.Count;
                        skip += batchRecords.Count;
                        
                        // Se riceviamo esattamente maxPageSize record, potrebbero esserci altri record
                        if (batchRecords.Count == maxPageSize)
                        {
                            // Continua a paginare, ma limita a un massimo di iterazioni per evitare loop infiniti
                            var maxIterations = 10; // Massimo 10 iterazioni = 500000 record
                            hasMore = (skip / maxPageSize) < maxIterations;
                            _logger.LogDebug("Ricevuti {MaxPageSize} record (maxpagesize), continuo paginazione con skip={Skip}", maxPageSize, skip);
                        }
                        else if (batchRecords.Count < maxPageSize)
                        {
                            _logger.LogDebug("Ricevuti meno di {MaxPageSize} record ({Count}), fine paginazione", maxPageSize, batchRecords.Count);
                            hasMore = false;
                        }
                        else
                        {
                            hasMore = true;
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Nessun record ricevuto, fine paginazione");
                        hasMore = false;
                    }
                }
                else
                {
                    hasMore = false;
                }
            }

            return allRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting records from SAP for table: {TableName}", tableName);
            throw;
        }
    }

    public async Task<(List<T> Items, int TotalCount)> GetRecordsPagedAsync<T>(string tableName, int skip, int top, string? filter = null, string sessionId = "", string? orderBy = null)
    {
        try
        {
            var url = tableName;
            var queryParams = new List<string>();
            
            if (!string.IsNullOrEmpty(filter))
            {
                queryParams.Add($"$filter={Uri.EscapeDataString(filter)}");
            }
            
            // Aggiungi $skip e $top per la paginazione
            queryParams.Add($"$skip={skip}");
            queryParams.Add($"$top={top}");
            
            // Aggiungi $count per ottenere il totale
            queryParams.Add("$count=true");

            // Aggiungi $orderby se specificato
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                queryParams.Add($"$orderby={Uri.EscapeDataString(orderBy)}");
            }
            
            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams);
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            // Aggiungi l'header Prefer per impostare il maxpagesize
            request.Headers.Add("Prefer", "odata.maxpagesize=50000");
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                var cookieHeader = BuildCookieHeader(sessionId);
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    request.Headers.Add("Cookie", cookieHeader);
                }
            }

            var fullUrl = _httpClient.BaseAddress != null ? new Uri(_httpClient.BaseAddress, url).ToString() : url;
            _logger.LogDebug("SAP GET paged - relative URL: {RelativeUrl}, full URL: {FullUrl}, skip: {Skip}, top: {Top}", url, fullUrl, skip, top);
            var response = await _httpClient.SendAsync(request);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (new List<T>(), 0);
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("SAP GET paged failed. Relative URL: {RelativeUrl}, Full URL: {FullUrl}, Status: {Status}, Body: {Body}", url, fullUrl, response.StatusCode, errorBody);
                var ex = new HttpRequestException($"SAP GET paged {url} failed. Status: {response.StatusCode}. Body: {errorBody}");
                ex.Data["StatusCode"] = response.StatusCode;
                throw ex;
            }

            var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
            var items = new List<T>();
            var totalCount = 0;

            if (jsonDoc != null)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                // Recupera il totale dal campo @odata.count o odata.count
                if (jsonDoc.RootElement.TryGetProperty("@odata.count", out var countProp))
                {
                    totalCount = countProp.GetInt32();
                }
                else if (jsonDoc.RootElement.TryGetProperty("odata.count", out countProp))
                {
                    totalCount = countProp.GetInt32();
                }
                
                // Check if it's OData format with "value" property
                if (jsonDoc.RootElement.TryGetProperty("value", out var valueArray))
                {
                    items = JsonSerializer.Deserialize<List<T>>(valueArray.GetRawText(), options) ?? new List<T>();
                }
                // Check if it's a direct array (UDO format)
                else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    items = JsonSerializer.Deserialize<List<T>>(jsonDoc.RootElement.GetRawText(), options) ?? new List<T>();
                    // Per gli UDO, il Service Layer potrebbe non supportare $count=true
                    // Se non abbiamo il count, recuperiamo tutti i record per contare (solo se necessario)
                    if (totalCount == 0 && skip == 0)
                    {
                        // Solo alla prima pagina, recuperiamo il totale contando tutti i record
                        // Questo è costoso ma necessario per gli UDO
                        var allRecords = await GetRecordsAsync<T>(tableName, filter, sessionId);
                        totalCount = allRecords.Count;
                        _logger.LogDebug("Recuperato totale record per UDO: {TotalCount}", totalCount);
                    }
                    else if (totalCount == 0)
                    {
                        // Se non è la prima pagina e non abbiamo il count, usiamo un valore approssimativo
                        // basato sul numero di record ricevuti
                        totalCount = items.Count + skip;
                        _logger.LogWarning("Count non disponibile per UDO, usando valore approssimativo: {TotalCount}", totalCount);
                    }
                }
            }

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting paged records from SAP for table: {TableName}", tableName);
            throw;
        }
    }

    public async Task<T> GetRecordAsync<T>(string tableName, string code, string sessionId = "")
    {
        try
        {
            var url = $"{tableName}('{code}')";
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(sessionId))
            {
                var cookieHeader = BuildCookieHeader(sessionId);
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    request.Headers.Add("Cookie", cookieHeader);
                }
            }

            _logger.LogDebug("SAP GET {Url}", url);
            var response = await _httpClient.SendAsync(request);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return default(T)!;
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("SAP GET {Url} failed. Status: {Status}. Body: {Body}", url, response.StatusCode, errorBody);
                var ex = new HttpRequestException($"SAP GET {url} failed. Status: {response.StatusCode}. Body: {errorBody}");
                ex.Data["StatusCode"] = response.StatusCode;
                throw ex;
            }

            var result = await response.Content.ReadFromJsonAsync<T>();
            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting record from SAP for table: {TableName}, code: {Code}", tableName, code);
            throw;
        }
    }

    public async Task<T> CreateRecordAsync<T>(string tableName, object record, string sessionId = "")
    {
        try
        {
            var url = tableName;

            // Log JSON payload for CREATE request
            var createJson = JsonSerializer.Serialize(
                record,
                new JsonSerializerOptions { PropertyNamingPolicy = null }
            );
            _logger.LogDebug("SAP POST {Url} payload: {Payload}", url, createJson);
            
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(record, options: new JsonSerializerOptions { PropertyNamingPolicy = null })
            };
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                var cookieHeader = BuildCookieHeader(sessionId);
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    request.Headers.Add("Cookie", cookieHeader);
                }
            }

            _logger.LogDebug("SAP POST {Url}", url);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("SAP POST {Url} failed. Status: {Status}. Body: {Body}", url, response.StatusCode, errorBody);
                throw new HttpRequestException($"SAP POST {url} failed. Status: {response.StatusCode}. Body: {errorBody}");
            }

            var result = await response.Content.ReadFromJsonAsync<T>();
            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating record in SAP for table: {TableName}", tableName);
            throw;
        }
    }

    public async Task<T> UpdateRecordAsync<T>(string tableName, string code, object record, string sessionId = "")
    {
        try
        {
            var url = $"{tableName}('{code}')";

            // Log JSON payload for UPDATE/PATCH request
            var updateJson = JsonSerializer.Serialize(
                record,
                new JsonSerializerOptions { PropertyNamingPolicy = null }
            );
            _logger.LogDebug("SAP PATCH {Url} payload: {Payload}", url, updateJson);
            
            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = JsonContent.Create(record, options: new JsonSerializerOptions { PropertyNamingPolicy = null })
            };
            
            if (!string.IsNullOrEmpty(sessionId))
            {
                var cookieHeader = BuildCookieHeader(sessionId);
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    request.Headers.Add("Cookie", cookieHeader);
                }
            }

            // Replace collections on PATCH instead of appending
            request.Headers.Add("B1S-ReplaceCollectionsOnPatch", "true");

            _logger.LogDebug("SAP PATCH {Url}", url);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("SAP PATCH {Url} failed. Status: {Status}. Body: {Body}", url, response.StatusCode, errorBody);
                var ex = new HttpRequestException($"SAP PATCH {url} failed. Status: {response.StatusCode}. Body: {errorBody}");
                ex.Data["StatusCode"] = response.StatusCode;
                throw ex;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent ||
                response.Content == null ||
                response.Content.Headers.ContentLength == 0)
            {
                return default!;
            }

            var result = await response.Content.ReadFromJsonAsync<T>();
            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating record in SAP for table: {TableName}, code: {Code}", tableName, code);
            throw;
        }
    }

    public async Task DeleteRecordAsync(string tableName, string code, string sessionId = "")
    {
        try
        {
            var url = $"{tableName}('{code}')";
            
            var request = new HttpRequestMessage(HttpMethod.Delete, url);
            if (!string.IsNullOrEmpty(sessionId))
            {
                var cookieHeader = BuildCookieHeader(sessionId);
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    request.Headers.Add("Cookie", cookieHeader);
                }
            }

            _logger.LogDebug("SAP DELETE {Url}", url);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("SAP DELETE {Url} failed. Status: {Status}. Body: {Body}", url, response.StatusCode, errorBody);
                var ex = new HttpRequestException($"SAP DELETE {url} failed. Status: {response.StatusCode}. Body: {errorBody}");
                ex.Data["StatusCode"] = response.StatusCode;
                throw ex;
            }
            
            _logger.LogInformation("Successfully deleted record from table: {TableName}, code: {Code}", tableName, code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting record from SAP for table: {TableName}, code: {Code}", tableName, code);
            throw;
        }
    }
}

