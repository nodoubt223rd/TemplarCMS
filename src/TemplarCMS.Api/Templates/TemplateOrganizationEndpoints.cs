using Microsoft.AspNetCore.Mvc;
using TemplarCMS.Application.Templates;
using TemplarCMS.Api.Security;
using TemplarCMS.ContentModeling.Organization;

namespace TemplarCMS.Api.Templates;

public sealed record TemplateFolderRequest(string? Name, string? Key, Guid? ParentId, Guid ExpectedRevision);
public sealed record TemplatePlacementRequest(string? Name, Guid? ParentId, Guid ExpectedRevision);

public static class TemplateOrganizationEndpoints
{
    public static IEndpointRouteBuilder MapTemplateOrganizationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/template-organization", async (TemplateOrganizationService service,
            JsonTemplateOrganizationRepository organization, TemplateMutationCoordinator coordinator,
            TemplarCMS.ContentModeling.Repositories.ITemplateRepository templates,
            TemplarCMS.ContentModeling.Definitions.IBuiltInTemplateProvider builtIns, CancellationToken ct) =>
        {
            await using var gate = await organization.AcquireAsync(ct);
            await coordinator.RecoverLockedAsync(ct);
            return Results.Ok(await TemplateOrganizationResponse.CreateAsync(await service.ReadAsync(ct), templates, builtIns, ct));
        }).WithName("GetTemplateOrganization").WithTags("Templates").Produces<TemplateOrganizationResponse>();
        Map(endpoints.MapPost("/api/v1/template-folders", async (TemplateFolderRequest request, TemplateOrganizationService service, CancellationToken ct) =>
            Results.Ok(await service.ChangeFolderAsync("create", null, request.Name, request.Key, request.ParentId, request.ExpectedRevision, ct))), "CreateTemplateFolder");
        Map(endpoints.MapPost("/api/v1/template-folders/{id:guid}/rename", async (Guid id, TemplateFolderRequest request, TemplateOrganizationService service, CancellationToken ct) =>
            Results.Ok(await service.ChangeFolderAsync("rename", id, request.Name, null, null, request.ExpectedRevision, ct))), "RenameTemplateFolder");
        Map(endpoints.MapPost("/api/v1/template-folders/{id:guid}/move", async (Guid id, TemplateFolderRequest request, TemplateOrganizationService service, CancellationToken ct) =>
            Results.Ok(await service.ChangeFolderAsync("move", id, null, null, request.ParentId, request.ExpectedRevision, ct))), "MoveTemplateFolder");
        Map(endpoints.MapDelete("/api/v1/template-folders/{id:guid}", async (Guid id, [FromQuery] Guid expectedRevision, TemplateOrganizationService service, CancellationToken ct) =>
            Results.Ok(await service.ChangeFolderAsync("delete", id, null, null, null, expectedRevision, ct))), "DeleteTemplateFolder");
        Map(endpoints.MapPost("/api/v1/templates/{id:guid}/move", async (Guid id, TemplatePlacementRequest request, TemplateOrganizationService service, CancellationToken ct) =>
            Results.Ok(await service.ChangeTemplateAsync(id, "move", null, request.ParentId, request.ExpectedRevision, ct))), "MoveTemplate");
        Map(endpoints.MapPost("/api/v1/templates/{id:guid}/rename", async (Guid id, TemplatePlacementRequest request, TemplateOrganizationService service, CancellationToken ct) =>
            Results.Ok(await service.ChangeTemplateAsync(id, "rename", request.Name, null, request.ExpectedRevision, ct))), "RenameTemplate");
        return endpoints;
    }

    private static void Map(RouteHandlerBuilder route, string name) => route.WithName(name).WithTags("Templates")
        .RequireAuthorization(ApiAuthorizationPolicies.AuthorContent).AddEndpointFilter<TemplateMutationFilter>()
        .Produces<TemplateOrganizationSnapshot>().ProducesProblem(400).ProducesProblem(401).ProducesProblem(403)
        .ProducesProblem(404).ProducesProblem(409);
}
