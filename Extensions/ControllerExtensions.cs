using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ADOTTA.Projects.Suite.Api.Extensions;

public static class ControllerExtensions
{
    public static ActionResult<T> HandleSapError<T>(this ControllerBase controller, Exception ex, ILogger logger, string operation)
    {
        // Se è un HttpRequestException, verifica lo status code
        if (ex is HttpRequestException httpEx)
        {
            // Verifica se lo status code è nel Data
            if (httpEx.Data.Contains("StatusCode"))
            {
                var statusCode = httpEx.Data["StatusCode"];
                if (statusCode is HttpStatusCode code)
                {
                    if (code == HttpStatusCode.Unauthorized)
                    {
                        logger.LogWarning("SAP Unauthorized error during {Operation}", operation);
                        return controller.StatusCode(401, new { message = "SAP session expired or invalid. Please login again.", error = "Unauthorized" });
                    }
                    // Restituisci lo stesso status code se è un errore HTTP
                    if ((int)code >= 400 && (int)code < 500)
                    {
                        logger.LogWarning("SAP client error ({StatusCode}) during {Operation}", code, operation);
                        return controller.StatusCode((int)code, new { message = $"SAP error during {operation}", error = ex.Message });
                    }
                }
            }
        }

        // Verifica se il messaggio contiene "401" o "Unauthorized"
        if (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("SAP Unauthorized error during {Operation}", operation);
            return controller.StatusCode(401, new { message = "SAP session expired or invalid. Please login again.", error = "Unauthorized" });
        }

        // Per tutti gli altri errori, restituisci 500
        logger.LogError(ex, "Error during {Operation}", operation);
        return controller.StatusCode(500, new { message = $"Error during {operation}", error = ex.Message });
    }
}

