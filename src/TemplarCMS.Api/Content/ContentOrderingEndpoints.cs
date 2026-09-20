using TemplarCMS.Api.Security;
using TemplarCMS.Application.Content;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.Api.Content;

public sealed record ContentReorderRequest(string Direction, string ExpectedRevision);

public static class ContentOrderingEndpoints
{
    public static IEndpointRouteBuilder MapContentOrderingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/content-order", async (Guid? parentId, ContentOrderingService service, CancellationToken ct) =>
            Results.Ok(await service.ReadAsync(parentId, ct))).WithName("GetContentOrder").WithTags("Content").Produces<ContentOrderSnapshot>();
        endpoints.MapPost("/api/v1/content/{id:guid}/reorder", async (Guid id, ContentReorderRequest request, ContentOrderingService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.ReorderAsync(id, request.Direction, request.ExpectedRevision, ct)) as IResult; }
            catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException or ContentOrderConflictException)
            {
                return Results.Problem(detail: exception.Message, title: "Content ordering failed", statusCode: exception switch
                { KeyNotFoundException => 404, ContentOrderConflictException => 409, _ => 400 }, type: "/api/problems/content-order-conflict");
            }
        }).WithName("ReorderContent").WithTags("Content").RequireAuthorization(ApiAuthorizationPolicies.AuthorContent)
            .Produces<ContentOrderSnapshot>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403).ProducesProblem(404).ProducesProblem(409);
        return endpoints;
    }
}
