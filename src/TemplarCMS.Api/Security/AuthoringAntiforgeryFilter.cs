using Microsoft.AspNetCore.Antiforgery;

namespace TemplarCMS.Api.Security;

public sealed class AuthoringAntiforgeryFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (HttpMethods.IsGet(context.HttpContext.Request.Method) || HttpMethods.IsHead(context.HttpContext.Request.Method)) return await next(context);
        try { await antiforgery.ValidateRequestAsync(context.HttpContext); }
        catch (AntiforgeryValidationException) { return TypedResults.Problem(ApiProblems.Auth(400, "antiforgery-failed", "Refresh the session and try again.")); }
        return await next(context);
    }
}
