namespace ADOTTA.Projects.Suite.Api.Services;

public interface ISAPServiceLayerClient
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync(string sessionId);
    Task<List<T>> GetRecordsAsync<T>(string tableName, string? filter = null, string sessionId = "");
    Task<(List<T> Items, int TotalCount)> GetRecordsPagedAsync<T>(string tableName, int skip, int top, string? filter = null, string sessionId = "", string? orderBy = null);
    Task<T> GetRecordAsync<T>(string tableName, string code, string sessionId = "");
    Task<T> CreateRecordAsync<T>(string tableName, object record, string sessionId = "");
    Task<T> UpdateRecordAsync<T>(string tableName, string code, object record, string sessionId = "");
    Task DeleteRecordAsync(string tableName, string code, string sessionId = "");
}

public class LoginRequest
{
    public string CompanyDB { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string SessionId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int SessionTimeout { get; set; }
}

