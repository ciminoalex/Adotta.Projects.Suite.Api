using System.Text.Json;
using ADOTTA.Projects.Suite.Api.DTOs;

namespace ADOTTA.Projects.Suite.Api.Services;

public class UserService : IUserService
{
    private const string TableName = "AX_ADT_USERS";
    private readonly ISAPServiceLayerClient _sapClient;
    private readonly ILogger<UserService> _logger;

    public UserService(ISAPServiceLayerClient sapClient, ILogger<UserService> logger)
    {
        _sapClient = sapClient;
        _logger = logger;
    }

    public async Task<List<UserDto>> GetUsersAsync(string? query, string sessionId)
    {
        string? filter = null;
        if (!string.IsNullOrWhiteSpace(query))
        {
            var safe = query.Replace("'", "''");
            filter = $"contains(U_Username, '{safe}') or contains(Name, '{safe}') or contains(U_Email, '{safe}')";
        }

        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(TableName, filter, sessionId);
        return sapData.Select(record => MapToUserDto(record)).ToList();
    }

    public async Task<UserDto?> GetUserByIdAsync(string code, string sessionId)
    {
        try
        {
            var record = await _sapClient.GetRecordAsync<JsonElement>(TableName, code, sessionId);
            if (record.ValueKind == JsonValueKind.Undefined || record.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            return MapToUserDto(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserCode}", code);
            return null;
        }
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email, string sessionId, bool includeSensitive = false)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var safeEmail = email.Replace("'", "''");
        var filter = $"U_Email eq '{safeEmail}'";
        var sapData = await _sapClient.GetRecordsAsync<JsonElement>(TableName, filter, sessionId);
        var first = sapData.FirstOrDefault();
        if (first.ValueKind == JsonValueKind.Undefined || first.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var user = MapToUserDto(first, includeSensitive);
        return string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase) ? user : null;
    }

    public async Task<UserDto> CreateUserAsync(UserDto user, string sessionId)
    {
        var sapPayload = MapToSapRecord(user, isUpdate: false);
        var created = await _sapClient.CreateRecordAsync<JsonElement>(TableName, sapPayload, sessionId);
        return MapToUserDto(created);
    }

    public async Task<UserDto> UpdateUserAsync(string code, UserDto user, string sessionId)
    {
        user.Code = code;
        var sapPayload = MapToSapRecord(user, isUpdate: true);
        var updated = await _sapClient.UpdateRecordAsync<JsonElement>(TableName, code, sapPayload, sessionId);
        return MapToUserDto(updated);
    }

    public async Task DeleteUserAsync(string code, string sessionId)
    {
        await _sapClient.DeleteRecordAsync(TableName, code, sessionId);
    }

    private UserDto MapToUserDto(JsonElement record, bool includeSensitive = false)
    {
        return new UserDto
        {
            Code = record.TryGetProperty("Code", out var codeProp) ? codeProp.GetString() ?? string.Empty : string.Empty,
            UserCode = record.TryGetProperty("U_Username", out var username) ? username.GetString() ?? string.Empty : string.Empty,
            Email = record.TryGetProperty("U_Email", out var email) ? email.GetString() ?? string.Empty : string.Empty,
            UserName = record.TryGetProperty("Name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
            Ruolo = record.TryGetProperty("U_Ruolo", out var role) ? role.GetString() ?? string.Empty : string.Empty,
            TeamTecnico = record.TryGetProperty("U_TeamTecnico", out var team) ? team.GetString() : null,
            IsActive = record.TryGetProperty("U_IsActive", out var isActive)
                ? string.Equals(isActive.GetString(), "Y", StringComparison.OrdinalIgnoreCase)
                : true,
            Password = includeSensitive && record.TryGetProperty("U_Password", out var password)
                ? password.GetString()
                : null
        };
    }

    private object MapToSapRecord(UserDto user, bool isUpdate)
    {
        var code = !string.IsNullOrWhiteSpace(user.Code) ? user.Code : Guid.NewGuid().ToString("N");
        var payload = new Dictionary<string, object?>
        {
            ["Code"] = isUpdate ? user.Code : code,
            ["Name"] = user.UserName,
            ["U_Username"] = user.UserCode,
            ["U_Email"] = user.Email,
            ["U_Ruolo"] = user.Ruolo,
            ["U_TeamTecnico"] = user.TeamTecnico,
            ["U_IsActive"] = user.IsActive ? "Y" : "N"
        };

        if (!string.IsNullOrWhiteSpace(user.Password))
        {
            payload["U_Password"] = user.Password;
        }

        return payload;
    }
}

