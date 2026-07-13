using Microsoft.AspNetCore.Http.HttpResults;
using TemplarCMS.Api.Content;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Catalog;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;
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

        endpoints.MapPost(
                "/api/v1/templates",
                CreateAsync)
            .WithName("CreateTemplate")
            .WithTags("Templates")
            .Produces<TemplateResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

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

    public static async Task<Results<Created<TemplateResponse>, ProblemHttpResult>> CreateAsync(
        CreateTemplateRequest? request,
        ITemplateRepository templateRepository,
        IContentModelCatalog contentModelCatalog,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(templateRepository);
        ArgumentNullException.ThrowIfNull(contentModelCatalog);

        if (request == null)
        {
            return TypedResults.Problem(
                title: "Template request is required",
                detail: "Provide a template payload in the request body.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        TemplateDefinition? template = null;

        try
        {
            template =
                MapCreateRequest(
                    request);

            await templateRepository.CreateTemplateAsync(
                template,
                cancellationToken);

            try
            {
                await contentModelCatalog.RefreshAsync(
                    cancellationToken);
            }
            catch
            {
                await RollbackCreateAsync(
                    templateRepository,
                    contentModelCatalog,
                    template.Key,
                    cancellationToken);

                throw;
            }

            var createdTemplate =
                await contentModelCatalog.GetEffectiveTemplateAsync(
                    template.Id,
                    cancellationToken);

            if (createdTemplate == null)
            {
                return TypedResults.Problem(
                    title: "Created template could not be loaded",
                    detail: $"Template '{template.Id}' was saved but could not be reloaded.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var location =
                $"/api/v1/templates/{createdTemplate.Id.Value}";

            return TypedResults.Created(
                location,
                MapResponse(createdTemplate));
        }
        catch (ContentModelCatalogRefreshException exception)
        {
            var detail =
                exception.Errors.Count == 0
                    ? exception.Message
                    : string.Join(
                        " ",
                        exception.Errors.Select(error => error.Message));

            return TypedResults.Problem(
                title: "Template could not be created",
                detail: detail,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (InvalidOperationException exception)
        {
            var statusCode =
                exception.Message.Contains(
                    "already exists",
                    StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;

            return TypedResults.Problem(
                title: statusCode == StatusCodes.Status409Conflict
                    ? "Template could not be created"
                    : "Invalid template create request",
                detail: exception.Message,
                statusCode: statusCode);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.Problem(
                title: "Invalid template create request",
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

    private static TemplateDefinition MapCreateRequest(
        CreateTemplateRequest request)
    {
        if (request.Sections == null)
        {
            throw new ArgumentException(
                "Template sections are required.",
                nameof(request));
        }

        return new TemplateDefinition(
            new TemplateId(Guid.NewGuid()),
            request.Name,
            new TemplateKey(request.Key),
            sections: request.Sections
                .Select(
                    section => new TemplateSectionDefinition(
                        Guid.NewGuid(),
                        section.Name,
                        section.Key,
                        section.SortOrder,
                        (section.Fields ?? throw new ArgumentException(
                            $"Template section '{section.Key}' fields are required.",
                            nameof(request)))
                            .Select(
                                field => new FieldDefinition(
                                    new FieldId(Guid.NewGuid()),
                                    field.Name,
                                    field.Key,
                                    ParseFieldType(field.Type),
                                    field.IsShared,
                                    field.IsUnversioned,
                                    field.Metadata))
                            .ToArray()))
                .ToArray());
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

    private static FieldType ParseFieldType(
        string? fieldType)
    {
        if (string.IsNullOrWhiteSpace(fieldType))
        {
            throw new InvalidOperationException(
                "Field type is required.");
        }

        return fieldType.Trim().ToLowerInvariant() switch
        {
            "singlelinetext" => FieldType.SingleLineText,
            "multilinetext" => FieldType.MultiLineText,
            "richtext" => FieldType.RichText,
            "checkbox" => FieldType.Checkbox,
            "datetime" => FieldType.DateTime,
            "integer" => FieldType.Integer,
            "decimal" => FieldType.Decimal,
            "droplink" => FieldType.Droplink,
            "multilist" => FieldType.Multilist,
            "image" => FieldType.Image,
            "file" => FieldType.File,
            "json" => FieldType.Json,
            _ => throw new InvalidOperationException(
                $"Unsupported field type '{fieldType}'.")
        };
    }

    private static async Task RollbackCreateAsync(
        ITemplateRepository templateRepository,
        IContentModelCatalog contentModelCatalog,
        TemplateKey key,
        CancellationToken cancellationToken)
    {
        await templateRepository.DeleteTemplateAsync(
            key,
            cancellationToken);

        try
        {
            await contentModelCatalog.RefreshAsync(
                cancellationToken);
        }
        catch
        {
            // Preserve the original create failure; the caller will still receive it.
        }
    }
}
