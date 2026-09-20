using TemplarCMS.Application.Templates;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Organization;

namespace TemplarCMS.Api.Templates;

public sealed class TemplateMutationFilter(TemplateMutationCoordinator coordinator,
    TemplateOrganizationService organization, IContentModelCatalog catalog) : IEndpointFilter
{
    private sealed class RejectedResult(object? result) : Exception { public object? Result { get; } = result; }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        object? result = null;
        try
        {
            await coordinator.ExecuteAsync(async ct =>
            {
                await catalog.RefreshAsync(ct);
                var before = await organization.ReadAsync(ct);
                var create = context.Arguments.OfType<CreateTemplateRequest>().FirstOrDefault();
                if (create?.ExpectedOrganizationRevision is { } revision)
                    TemplateOrganizationService.CheckRevision(before, revision);
                if (create?.ParentFolderId is { } parent)
                {
                    if (create.ExpectedOrganizationRevision is null) throw new ArgumentException("Organization revision is required when creating within a folder.");
                    if (!before.Folders.Any(f => f.Id.Value == parent)) throw new KeyNotFoundException("Destination folder was not found.");
                }
                result = await next(context);
                var actual = result is INestedHttpResult nested ? nested.Result : result;
                if (actual is IStatusCodeHttpResult { StatusCode: >= 400 }) throw new RejectedResult(result);
                // Definition creation/deletion and organization placement commit as one operation.
                if (create is not null || HttpMethods.IsDelete(context.HttpContext.Request.Method) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/api/v1/templates"))
                {
                    var definitions = await catalog.GetEffectiveTemplatesAsync(ct);
                    var ids = definitions.Select(t => t.Id).ToHashSet();
                    var placements = before.Placements.Where(p => ids.Contains(p.TemplateId)).ToList();
                    if (create?.ParentFolderId is { } folder && actual is IValueHttpResult { Value: TemplateResponse created })
                        placements.Add(new(new(Guid.Parse(created.Id)), new(folder)));
                    await organization.SaveAsync(before with { Placements = placements }, ct);
                }
                await catalog.RefreshAsync(ct);
                context.HttpContext.Response.Headers["X-Template-Organization-Revision"] =
                    (await organization.ReadAsync(ct)).Revision.ToString();
            }, context.HttpContext.RequestAborted);
            return result;
        }
        catch (RejectedResult rejected)
        {
            await catalog.RefreshAsync(CancellationToken.None);
            return rejected.Result;
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException or TemplateOrganizationConflictException)
        {
            await catalog.RefreshAsync(CancellationToken.None);
            return Results.Problem(detail: exception.Message, statusCode: exception switch
            {
                KeyNotFoundException => 404,
                TemplateOrganizationConflictException => 409,
                _ => 400
            }, title: "Template organization operation failed", type: "/api/problems/template-organization-conflict");
        }
        catch
        {
            await catalog.RefreshAsync(CancellationToken.None);
            throw;
        }
    }
}
