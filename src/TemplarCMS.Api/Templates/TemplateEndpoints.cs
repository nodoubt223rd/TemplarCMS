using Microsoft.AspNetCore.Http.HttpResults;
using TemplarCMS.Api.Content;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.Api.Templates;

public static class TemplateEndpoints
{
    public static IEndpointRouteBuilder MapTemplateEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(
                "/api/v1/templates/{id:guid}",
                GetByIdAsync)
            .WithName("GetTemplateById")
            .WithTags("Templates")
            .Produces<TemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    public static async Task<Results<Ok<TemplateResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        IContentModelCatalog contentModelCatalog,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentModelCatalog);

        try
        {
            var template =
                await contentModelCatalog.GetEffectiveTemplateAsync(
                    new TemplateId(id),
                    cancellationToken);

            if (template == null)
            {
                return TypedResults.Problem(
                    title: "Template was not found",
                    detail: $"No template exists with id '{id}'.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            return TypedResults.Ok(
                MapResponse(template));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.Problem(
                title: "Invalid template lookup request",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static TemplateResponse MapResponse(
        EffectiveTemplateDefinition template)
    {
        return new TemplateResponse
        {
            Id = template.Id.Value.ToString(),
            Name = template.Name,
            Key = template.Key.ToString(),
            Sections = template.Sections
                .Select(
                    section => new TemplateSectionResponse
                    {
                        Id = section.Id.ToString(),
                        Name = section.Name,
                        Key = section.Key,
                        SortOrder = section.SortOrder,
                        Fields = section.Fields
                            .Select(
                                field => new TemplateFieldResponse
                                {
                                    Id = field.Id.Value.ToString(),
                                    Name = field.Name,
                                    Key = field.Key,
                                    Type = field.FieldType.ToString(),
                                    IsShared = field.IsShared,
                                    IsUnversioned = field.IsUnversioned
                                })
                            .ToArray()
                    })
                .ToArray(),
            Links = new TemplateLinksResponse
            {
                Self = new LinkResponse
                {
                    Href = $"/api/v1/templates/{template.Id.Value}"
                },
                Fields = new LinkResponse
                {
                    Href = $"/api/v1/templates/{template.Id.Value}/fields"
                },
                CreateItem = new LinkResponse
                {
                    Href = "/api/v1/content"
                }
            }
        };
    }
}
