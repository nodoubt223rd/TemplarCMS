using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TemplarCMS.Application.Content;
using TemplarCMS.Abstractions.Content;
using TemplarCMS.Api;
using TemplarCMS.Api.Content;
using TemplarCMS.Api.Security;
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
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(ApiAuthorizationPolicies.AuthorContent);

        endpoints.MapPut(
                "/api/v1/templates/{id:guid}",
                UpdateAsync)
            .WithName("UpdateTemplate")
            .WithTags("Templates")
            .Produces<TemplateResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization(ApiAuthorizationPolicies.AuthorContent);

        endpoints.MapDelete(
                "/api/v1/templates/{id:guid}",
                DeleteAsync)
            .WithName("DeleteTemplate")
            .WithTags("Templates")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization(ApiAuthorizationPolicies.AuthorContent);

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

        endpoints.MapGet(
                "/api/v1/templates/{id:guid}/dependencies",
                GetDependenciesByIdAsync)
            .WithName("GetTemplateDependenciesById")
            .WithTags("Templates")
            .Produces<TemplateDependencyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    public static async Task<Ok<TemplateCollectionResponse>> GetAllAsync(
        [FromServices] IContentModelCatalog contentModelCatalog,
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
        [FromServices] IContentModelCatalog contentModelCatalog,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentModelCatalog);

        try
        {
            var template =
                await contentModelCatalog.GetTemplateAsync(
                    new TemplateId(id),
                    cancellationToken);

            if (template == null)
            {
                return ApiProblems.TemplateNotFound(id);
            }

            return TypedResults.Ok(
                MapResponse(template));
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidTemplateLookupRequest(exception.Message);
        }
    }

    public static async Task<Results<Created<TemplateResponse>, ProblemHttpResult>> CreateAsync(
        CreateTemplateRequest? request,
        [FromServices] ITemplateRepository templateRepository,
        [FromServices] IContentModelCatalog contentModelCatalog,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(templateRepository);
        ArgumentNullException.ThrowIfNull(contentModelCatalog);

        if (request == null)
        {
            return ApiProblems.TemplateRequestRequired();
        }

        TemplateDefinition? template = null;

        try
        {
            template =
                await MapCreateRequestAsync(
                    request,
                    contentModelCatalog,
                    cancellationToken);

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
                await contentModelCatalog.GetTemplateAsync(
                    template.Id,
                    cancellationToken);

            if (createdTemplate == null)
            {
                return ApiProblems.CreatedTemplateCouldNotBeLoaded(template.Id);
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

            return ApiProblems.TemplateCouldNotBeCreated(detail, StatusCodes.Status400BadRequest);
        }
        catch (InvalidOperationException exception)
        {
            var statusCode =
                exception.Message.Contains(
                    "already exists",
                    StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;

            return ApiProblems.TemplateCouldNotBeCreated(exception.Message, statusCode);
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidTemplateCreateRequest(exception.Message);
        }
    }

    public static async Task<Results<Ok<TemplateResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        CreateTemplateRequest? request,
        [FromServices] ITemplateRepository templateRepository,
        [FromServices] IContentModelCatalog contentModelCatalog,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(templateRepository);
        ArgumentNullException.ThrowIfNull(contentModelCatalog);

        if (request == null)
        {
            return ApiProblems.TemplateRequestRequired();
        }

        TemplateDefinition? existingTemplate = null;
        TemplateDefinition? updatedTemplate = null;

        try
        {
            existingTemplate =
                await contentModelCatalog.GetTemplateAsync(
                    new TemplateId(id),
                    cancellationToken);

            if (existingTemplate == null)
            {
                return ApiProblems.TemplateNotFound(id);
            }

            updatedTemplate =
                await MapTemplateRequestAsync(
                    existingTemplate.Id,
                    request,
                    contentModelCatalog,
                    cancellationToken);

            await templateRepository.UpdateTemplateAsync(
                existingTemplate.Key,
                updatedTemplate,
                cancellationToken);

            try
            {
                await contentModelCatalog.RefreshAsync(
                    cancellationToken);
            }
            catch
            {
                await RollbackUpdateAsync(
                    templateRepository,
                    contentModelCatalog,
                    existingTemplate,
                    updatedTemplate.Key,
                    cancellationToken);

                throw;
            }

            var refreshedTemplate =
                await contentModelCatalog.GetTemplateAsync(
                    existingTemplate.Id,
                    cancellationToken);

            if (refreshedTemplate == null)
            {
                return ApiProblems.UpdatedTemplateCouldNotBeLoaded(existingTemplate.Id);
            }

            return TypedResults.Ok(
                MapResponse(refreshedTemplate));
        }
        catch (ContentModelCatalogRefreshException exception)
        {
            var detail =
                exception.Errors.Count == 0
                    ? exception.Message
                    : string.Join(
                        " ",
                        exception.Errors.Select(error => error.Message));

            return ApiProblems.TemplateCouldNotBeUpdated(detail, StatusCodes.Status400BadRequest);
        }
        catch (InvalidOperationException exception)
        {
            var statusCode =
                exception.Message.Contains(
                    "already exists",
                    StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status409Conflict
                    : exception.Message.Contains(
                        "was not found",
                        StringComparison.OrdinalIgnoreCase)
                        ? StatusCodes.Status404NotFound
                        : StatusCodes.Status400BadRequest;

            return ApiProblems.TemplateCouldNotBeUpdated(exception.Message, statusCode);
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidTemplateUpdateRequest(exception.Message);
        }
    }

    public static async Task<Results<Ok<TemplateFieldCollectionResponse>, ProblemHttpResult>> GetFieldsByIdAsync(
        Guid id,
        [FromServices] IContentModelCatalog contentModelCatalog,
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
                return ApiProblems.TemplateNotFound(id);
            }

            return TypedResults.Ok(
                MapFieldCollectionResponse(template));
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidTemplateLookupRequest(exception.Message);
        }
    }

    public static async Task<Results<Ok<TemplateDependencyResponse>, ProblemHttpResult>> GetDependenciesByIdAsync(
        Guid id,
        [FromServices] ITemplateRepository templateRepository,
        [FromServices] IContentModelCatalog contentModelCatalog,
        [FromServices] IContentRepository contentRepository,
        [FromServices] IContentPathResolver contentPathResolver,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(templateRepository);
        ArgumentNullException.ThrowIfNull(contentModelCatalog);
        ArgumentNullException.ThrowIfNull(contentRepository);
        ArgumentNullException.ThrowIfNull(contentPathResolver);

        try
        {
            var template =
                await contentModelCatalog.GetTemplateAsync(
                    new TemplateId(id),
                    cancellationToken);

            if (template == null)
            {
                return ApiProblems.TemplateNotFound(id);
            }

            var templates =
                await templateRepository.GetTemplatesAsync(
                    cancellationToken);
            var dependentTemplates =
                GetDependentTemplates(
                    template.Id,
                    templates);
            var dependentItems =
                await contentRepository.GetItemsByTemplateAsync(
                    template.Id,
                    cancellationToken);
            var itemPaths =
                await contentPathResolver.ResolveAsync(
                    dependentItems,
                    cancellationToken);

            return TypedResults.Ok(
                new TemplateDependencyResponse
                {
                    TemplateId = template.Id.Value.ToString(),
                    TemplateKey = template.Key.ToString(),
                    CanDelete = dependentTemplates.Count == 0 && dependentItems.Count == 0,
                    Summary = new TemplateDependencySummaryResponse
                    {
                        DependentTemplateCount = dependentTemplates.Count,
                        DependentContentItemCount = dependentItems.Count
                    },
                    Embedded = new TemplateDependencyEmbeddedResponse
                    {
                        Templates = dependentTemplates
                            .Select(MapDependencyTemplateResponse)
                            .ToArray(),
                        ContentItems = dependentItems
                            .Select(
                                item => MapDependencyContentItemResponse(
                                    item,
                                    itemPaths[item.Id]))
                            .OrderBy(item => item.Path, StringComparer.Ordinal)
                            .ToArray()
                    },
                    Links = new TemplateDependencyLinksResponse
                    {
                        Self = new LinkResponse
                        {
                            Href = $"/api/v1/templates/{template.Id.Value}/dependencies"
                        },
                        Template = new LinkResponse
                        {
                            Href = $"/api/v1/templates/{template.Id.Value}"
                        }
                    }
                });
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidTemplateLookupRequest(exception.Message);
        }
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        Guid id,
        [FromServices] ITemplateRepository templateRepository,
        [FromServices] IContentModelCatalog contentModelCatalog,
        [FromServices] IContentRepository contentRepository,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(templateRepository);
        ArgumentNullException.ThrowIfNull(contentModelCatalog);
        ArgumentNullException.ThrowIfNull(contentRepository);

        TemplateDefinition? template = null;

        try
        {
            template =
                await contentModelCatalog.GetTemplateAsync(
                    new TemplateId(id),
                    cancellationToken);

            if (template == null)
            {
                return ApiProblems.TemplateNotFound(id);
            }

            var dependenciesHref =
                $"/api/v1/templates/{template.Id.Value}/dependencies";

            var templates =
                await templateRepository.GetTemplatesAsync(
                    cancellationToken);
            var dependentTemplate =
                templates.FirstOrDefault(
                    candidate => candidate.BaseTemplate?.Id == template.Id);

            if (dependentTemplate != null)
            {
                return ApiProblems.TemplateDeleteConflict(
                    $"Template '{template.Key}' is used as a base template by '{dependentTemplate.Key}'. Review the dependency snapshot before retrying after removing dependent templates.",
                    dependenciesHref);
            }

            var dependentItems =
                await contentRepository.GetItemsByTemplateAsync(
                    template.Id,
                    cancellationToken);

            if (dependentItems.Count > 0)
            {
                return ApiProblems.TemplateDeleteConflict(
                    $"Template '{template.Key}' is still assigned to one or more content items. Review the dependency snapshot before retrying after reassigning those items.",
                    dependenciesHref);
            }

            await templateRepository.DeleteTemplateAsync(
                template.Key,
                cancellationToken);

            try
            {
                await contentModelCatalog.RefreshAsync(
                    cancellationToken);
            }
            catch
            {
                await RollbackDeleteAsync(
                    templateRepository,
                    contentModelCatalog,
                    template,
                    cancellationToken);

                throw;
            }

            return TypedResults.NoContent();
        }
        catch (ContentModelCatalogRefreshException exception)
        {
            var detail =
                exception.Errors.Count == 0
                    ? exception.Message
                    : string.Join(
                        " ",
                        exception.Errors.Select(error => error.Message));

            return ApiProblems.TemplateCouldNotBeDeleted(detail);
        }
        catch (InvalidOperationException exception)
        {
            var statusCode =
                exception.Message.Contains(
                    "was not found",
                    StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest;

            return ApiProblems.TemplateCouldNotBeDeleted(exception.Message, statusCode);
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidTemplateDeleteRequest(exception.Message);
        }
    }

    private static TemplateResponse MapResponse(
        TemplateDefinition template)
    {
        return new TemplateResponse
        {
            Id = template.Id.Value.ToString(),
            Name = template.Name,
            Key = template.Key.ToString(),
            Icon = template.Icon ?? "file",
            BaseTemplate = template.BaseTemplate == null
                ? null
                : new TemplateBaseTemplateResponse
                {
                    Id = template.BaseTemplate.Id.Value.ToString(),
                    Name = template.BaseTemplate.Name,
                    Key = template.BaseTemplate.Key.ToString(),
                    Links = new TemplateBaseTemplateLinksResponse
                    {
                        Self = new LinkResponse
                        {
                            Href = $"/api/v1/templates/{template.BaseTemplate.Id.Value}"
                        }
                    }
                },
            Sections = template.Sections
                .Select(
                    section => new TemplateSectionResponse
                    {
                        Id = section.Id.ToString(),
                        Name = section.Name,
                        Key = section.Key,
                        SortOrder = section.SortOrder,
                        Metadata = section.Metadata.Count == 0
                            ? null
                            : section.Metadata,
                        Fields = section.Fields
                            .Select(
                                field => new TemplateFieldResponse
                                {
                                    Id = field.Id.Value.ToString(),
                                    Name = field.Name,
                                    Key = field.Key,
                                    Type = field.FieldType.ToString(),
                                    IsShared = field.IsShared,
                                    IsUnversioned = field.IsUnversioned,
                                    Metadata = field.Metadata.Count == 0
                                        ? null
                                        : field.Metadata
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
                Dependencies = new LinkResponse
                {
                    Href = $"/api/v1/templates/{template.Id.Value}/dependencies"
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
            Icon = template.Icon,
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
                Dependencies = new LinkResponse
                {
                    Href = $"/api/v1/templates/{template.Id.Value}/dependencies"
                },
                CreateItem = new LinkResponse
                {
                    Href = "/api/v1/content"
                }
            }
        };
    }

    private static Task<TemplateDefinition> MapCreateRequestAsync(
        CreateTemplateRequest request,
        IContentModelCatalog contentModelCatalog,
        CancellationToken cancellationToken)
    {
        return MapTemplateRequestAsync(
            new TemplateId(Guid.NewGuid()),
            request,
            contentModelCatalog,
            cancellationToken);
    }

    private static async Task<TemplateDefinition> MapTemplateRequestAsync(
        TemplateId templateId,
        CreateTemplateRequest request,
        IContentModelCatalog contentModelCatalog,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentModelCatalog);

        if (request.Sections == null)
        {
            throw new ArgumentException(
                "Template sections are required.",
                nameof(request));
        }

        TemplateDefinition? baseTemplate = null;

        if (request.BaseTemplateKeys != null && request.BaseTemplateKeys.Count > 1)
        {
            throw new InvalidOperationException(
                "Multiple base templates are not supported yet. Provide zero or one base template key.");
        }

        var baseTemplateKey =
            request.BaseTemplateKeys?.SingleOrDefault();

        if (!string.IsNullOrWhiteSpace(baseTemplateKey))
        {
            baseTemplate =
                await contentModelCatalog.GetTemplateAsync(
                    new TemplateKey(baseTemplateKey),
                    cancellationToken);

            if (baseTemplate == null)
            {
                throw new InvalidOperationException(
                    $"Base template '{baseTemplateKey.Trim()}' was not found.");
            }
        }

        return new TemplateDefinition(
            templateId,
            request.Name,
            new TemplateKey(request.Key),
            baseTemplate,
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
                .ToArray(),
            icon: AuthoringIconCatalog.Normalize(request.Icon));
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
                                Metadata = field.Metadata.Count == 0
                                    ? null
                                    : field.Metadata,
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
                Dependencies = new LinkResponse
                {
                    Href = $"/api/v1/templates/{template.Id.Value}/dependencies"
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
        return FieldTypeParser.Parse(fieldType);
    }

    private static IReadOnlyCollection<TemplateDefinition> GetDependentTemplates(
        TemplateId templateId,
        IReadOnlyCollection<TemplateDefinition> templates)
    {
        var dependentsByBaseTemplateId =
            templates
                .Where(candidate => candidate.BaseTemplate != null)
                .GroupBy(candidate => candidate.BaseTemplate!.Id)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToArray());
        var dependentTemplates =
            new List<TemplateDefinition>();
        var visited =
            new HashSet<TemplateId>();
        var pending =
            new Queue<TemplateId>();

        pending.Enqueue(templateId);

        while (pending.Count > 0)
        {
            var currentTemplateId = pending.Dequeue();

            if (!dependentsByBaseTemplateId.TryGetValue(currentTemplateId, out var directDependents))
            {
                continue;
            }

            foreach (var dependent in directDependents)
            {
                if (!visited.Add(dependent.Id))
                {
                    continue;
                }

                dependentTemplates.Add(dependent);
                pending.Enqueue(dependent.Id);
            }
        }

        return dependentTemplates
            .OrderBy(candidate => candidate.Key.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    private static TemplateDependencyTemplateItemResponse MapDependencyTemplateResponse(
        TemplateDefinition template)
    {
        return new TemplateDependencyTemplateItemResponse
        {
            Id = template.Id.Value.ToString(),
            Name = template.Name,
            Key = template.Key.ToString(),
            Links = new TemplateDependencyTemplateItemLinksResponse
            {
                Self = new LinkResponse
                {
                    Href = $"/api/v1/templates/{template.Id.Value}"
                }
            }
        };
    }

    private static TemplateDependencyContentItemResponse MapDependencyContentItemResponse(
        ContentItemDefinition item,
        ContentPath path)
    {
        return new TemplateDependencyContentItemResponse
        {
            Id = item.Id.Value.ToString(),
            Name = item.Name,
            Path = path.ToString(),
            Links = new TemplateDependencyContentItemLinksResponse
            {
                Self = new LinkResponse
                {
                    Href = $"/api/v1/content/{item.Id.Value}?lang=en&version=1"
                }
            }
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

    private static async Task RollbackDeleteAsync(
        ITemplateRepository templateRepository,
        IContentModelCatalog contentModelCatalog,
        TemplateDefinition template,
        CancellationToken cancellationToken)
    {
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
            // Preserve the original delete failure; the caller will still receive it.
        }
    }

    private static async Task RollbackUpdateAsync(
        ITemplateRepository templateRepository,
        IContentModelCatalog contentModelCatalog,
        TemplateDefinition originalTemplate,
        TemplateKey updatedTemplateKey,
        CancellationToken cancellationToken)
    {
        await templateRepository.UpdateTemplateAsync(
            updatedTemplateKey,
            originalTemplate,
            cancellationToken);

        try
        {
            await contentModelCatalog.RefreshAsync(
                cancellationToken);
        }
        catch
        {
            // Preserve the original update failure; the caller will still receive it.
        }
    }
}
