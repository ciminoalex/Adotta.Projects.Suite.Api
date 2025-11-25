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
            var url = tableName;
            if (!string.IsNullOrEmpty(filter))
            {
                url += $"?$filter={Uri.EscapeDataString(filter)}";
            }

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(sessionId))
            {
                var cookieHeader = BuildCookieHeader(sessionId);
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    request.Headers.Add("Cookie", cookieHeader);
                }
            }

            var fullUrl = _httpClient.BaseAddress != null ? new Uri(_httpClient.BaseAddress, url).ToString() : url;
            _logger.LogDebug("SAP GET relative URL: {RelativeUrl}, full URL: {FullUrl}", url, fullUrl);
            var response = await _httpClient.SendAsync(request);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new List<T>();
            }
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("SAP GET failed. Relative URL: {RelativeUrl}, Full URL: {FullUrl}, Status: {Status}, Body: {Body}", url, fullUrl, response.StatusCode, errorBody);
            }
            response.EnsureSuccessStatusCode();

            var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
            if (jsonDoc != null)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                // Check if it's OData format with "value" property
                if (jsonDoc.RootElement.TryGetProperty("value", out var valueArray))
                {
                    return JsonSerializer.Deserialize<List<T>>(valueArray.GetRawText(), options) ?? new List<T>();
                }
                
                // Check if it's a direct array (UDO format)
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<T>>(jsonDoc.RootElement.GetRawText(), options) ?? new List<T>();
                }
            }

            return new List<T>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting records from SAP for table: {TableName}", tableName);
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
            }
            
            response.EnsureSuccessStatusCode();

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

            _logger.LogDebug("SAP PATCH {Url}", url);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("SAP PATCH {Url} failed. Status: {Status}. Body: {Body}", url, response.StatusCode, errorBody);
            }
            response.EnsureSuccessStatusCode();

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
            }
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Successfully deleted record from table: {TableName}, code: {Code}", tableName, code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting record from SAP for table: {TableName}, code: {Code}", tableName, code);
            throw;
        }
    }
}

