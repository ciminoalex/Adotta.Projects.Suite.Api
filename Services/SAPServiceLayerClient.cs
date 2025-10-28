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
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
                // No need to check status; we only want Set-Cookie
            }
            catch (Exception pfEx)
            {
                _logger.LogDebug(pfEx, "Preflight request before SAP Login failed; continuing to Login");
            }

            var response = await _httpClient.PostAsJsonAsync("Login", request, serializeOptions);
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result != null)
                {
                    _logger.LogInformation("SAP Login successful. SessionId: {SessionId}", result.SessionId);
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
            var logoutUrl = $"{_httpClient.BaseAddress}/Logout";
            var request = new HttpRequestMessage(HttpMethod.Post, logoutUrl);
            request.Headers.Add("Cookie", $"B1SESSION={sessionId}");
            
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
                request.Headers.Add("Cookie", $"B1SESSION={sessionId}");
            }

            var response = await _httpClient.SendAsync(request);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new List<T>();
            }
            
            response.EnsureSuccessStatusCode();

            var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
            if (jsonDoc != null && jsonDoc.RootElement.TryGetProperty("value", out var valueArray))
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<T>>(valueArray.GetRawText(), options) ?? new List<T>();
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
                request.Headers.Add("Cookie", $"B1SESSION={sessionId}");
            }

            var response = await _httpClient.SendAsync(request);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return default(T)!;
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
                request.Headers.Add("Cookie", $"B1SESSION={sessionId}");
            }

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

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
                request.Headers.Add("Cookie", $"B1SESSION={sessionId}");
            }

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

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
                request.Headers.Add("Cookie", $"B1SESSION={sessionId}");
            }

            var response = await _httpClient.SendAsync(request);
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

