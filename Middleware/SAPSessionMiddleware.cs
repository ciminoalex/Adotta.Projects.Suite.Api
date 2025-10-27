namespace ADOTTA.Projects.Suite.Api.Middleware;

public class SAPSessionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SAPSessionMiddleware> _logger;

    public SAPSessionMiddleware(RequestDelegate next, ILogger<SAPSessionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Estrai SessionId dal header
        var sessionId = context.Request.Headers["X-SAP-Session-Id"].ToString();
        
        // Aggiungi SessionId al context per uso downstream
        if (!string.IsNullOrEmpty(sessionId))
        {
            context.Items["SAPSessionId"] = sessionId;
            _logger.LogDebug("SAP SessionId found: {SessionId}", sessionId);
        }
        else
        {
            _logger.LogDebug("No SAP SessionId found in request");
        }

        await _next(context);
    }
}

