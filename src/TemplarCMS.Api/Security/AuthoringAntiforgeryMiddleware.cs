using Microsoft.AspNetCore.Antiforgery;

namespace TemplarCMS.Api.Security;

/// <summary>Validates every API mutation after authentication and authorization, before body binding.</summary>
public sealed class AuthoringAntiforgeryMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext http, IAntiforgery antiforgery)
    {
        if (http.Request.Path.StartsWithSegments("/api/v1") &&
            http.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE")
        {
            try { await antiforgery.ValidateRequestAsync(http); }
            catch (AntiforgeryValidationException)
            {
                http.Response.StatusCode = 400;
                await http.Response.WriteAsJsonAsync(ApiProblems.Auth(400, "antiforgery-failed", "Refresh the session and try again."), options: null,
                    contentType: "application/problem+json", cancellationToken: http.RequestAborted);
                return;
            }
        }
        await next(http);
    }
}
