using ADOTTA.Projects.Suite.Api.Constants;
using Microsoft.AspNetCore.Http;

namespace ADOTTA.Projects.Suite.Api.Extensions;

public static class HttpContextExtensions
{
    public static string GetSapSessionId(this HttpContext context)
    {
        if (context.Items.TryGetValue("SAPSessionId", out var sessionObj) &&
            sessionObj is string storedSession &&
            !string.IsNullOrWhiteSpace(storedSession))
        {
            return storedSession;
        }

        var claimSession = context.User?.Claims
            .FirstOrDefault(c => string.Equals(c.Type, SapClaimTypes.SessionId, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (!string.IsNullOrWhiteSpace(claimSession))
        {
            context.Items["SAPSessionId"] = claimSession;
            return claimSession;
        }

        var headerValue = context.Request.Headers["X-SAP-Session-Id"].ToString();
        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            context.Items["SAPSessionId"] = headerValue;
            return headerValue;
        }

        return string.Empty;
    }
}


