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
                "/api/v1/templates",
                GetAllAsync)
            .WithName("GetTemplates")
            .WithTags("Templates")
            .Produces<TemplateCollectionResponse>(StatusCodes.Status200OK);

        endpoints.MapGet(
                "/api/v1/templates/{id:guid}",
                GetByIdAsync)
            .WithName("GetTemplateById")
            .WithTags("Templates")
            .Produces<TemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapGet(
                "/api/v1/templates/{id:guid}/fields",
                GetFieldsByIdAsync)
            .WithName("GetTemplateFieldsById")
            .WithTags("Templates")
            .Produces<TemplateFieldCollectionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    public static async Task<Ok<TemplateCollectionResponse>> GetAllAsync(
        IContentModelCatalog contentModelCatalog,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentModelCatalog);

        var templates =
            await contentModelCatalog.GetEffectiveTemplatesAsync(
                cancellationToken);

        return TypedResults.Ok(
            new TemplateCollectionResponse
            {
                Embedded = new TemplateCollectionEmbeddedResponse
                {
                    Templates = templates
                        .Select(MapSummaryResponse)
                        .ToArray()
                },
                Links = new TemplateCollectionLinksResponse
                {
                    Self = new LinkResponse
                    {
                        Href = "/api/v1/templates"
                    }
                }
            });
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

    public static async Task<Results<Ok<TemplateFieldCollectionResponse>, ProblemHttpResult>> GetFieldsByIdAsync(
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
                MapFieldCollectionResponse(template));
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

    private static TemplateSummaryResponse MapSummaryResponse(
        EffectiveTemplateDefinition template)
    {
        return new TemplateSummaryResponse
        {
            Id = template.Id.Value.ToString(),
            Name = template.Name,
            Key = template.Key.ToString(),
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

    private static TemplateFieldCollectionResponse MapFieldCollectionResponse(
        EffectiveTemplateDefinition template)
    {
        var fields =
            template.Sections
                .SelectMany(
                    section =>
                        section.Fields.Select(
                            field => new TemplateFieldItemResponse
                            {
                                Id = field.Id.Value.ToString(),
                                Name = field.Name,
                                Key = field.Key,
                                Type = field.FieldType.ToString(),
                                IsShared = field.IsShared,
                                IsUnversioned = field.IsUnversioned,
                                SectionId = section.Id.ToString(),
                                SectionName = section.Name,
                                SectionKey = section.Key,
                                SectionSortOrder = section.SortOrder
                            }))
                .ToArray();

        return new TemplateFieldCollectionResponse
        {
            Embedded = new TemplateFieldCollectionEmbeddedResponse
            {
                Fields = fields
            },
            Links = new TemplateFieldCollectionLinksResponse
            {
                Self = new LinkResponse
                {
                    Href = $"/api/v1/templates/{template.Id.Value}/fields"
                },
                Template = new LinkResponse
                {
                    Href = $"/api/v1/templates/{template.Id.Value}"
                },
                CreateItem = new LinkResponse
                {
                    Href = "/api/v1/content"
                }
            }
        };
    }
}
