namespace ADOTTA.Projects.Suite.Api.Services;

public interface IInitializationService
{
    Task<InitializationResult> InitializeAsync(string sessionId);
}

public sealed class InitializationResult
{
    public required List<string> Steps { get; init; }
    public required List<string> Warnings { get; init; }
}


