using ADOTTA.Projects.Suite.Api.Constants;

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
        var sessionId = context.User?.Claims.FirstOrDefault(c =>
            string.Equals(c.Type, SapClaimTypes.SessionId, StringComparison.OrdinalIgnoreCase))?.Value;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = context.Request.Headers["X-SAP-Session-Id"].ToString();
        }

        // Aggiungi SessionId al context per uso downstream
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            context.Items["SAPSessionId"] = sessionId;
            _logger.LogDebug("SAP SessionId found: {SessionId}", sessionId);
        }
        else
        {
            _logger.LogDebug("No SAP SessionId found in request");
        }

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("X-SAP-Session-Id"))
            {
                if (context.Items.TryGetValue("SAPSessionId", out var storedSessionObj)
                    && storedSessionObj is string storedSessionId
                    && !string.IsNullOrWhiteSpace(storedSessionId))
                {
                    context.Response.Headers["X-SAP-Session-Id"] = storedSessionId;
                }
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

