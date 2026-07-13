using Microsoft.AspNetCore.Http.HttpResults;
using TemplarCMS.Application.Content;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.Api.Content;

public static class ContentLookupEndpoints
{
    public static IEndpointRouteBuilder MapContentLookupEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(
                "/api/v1/content/{id:guid}",
                GetByIdAsync)
            .WithName("GetContentById")
            .WithTags("Content")
            .Produces<ContentItemResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapGet(
                "/api/v1/content/by-path/{**path}",
                GetByPathAsync)
            .WithName("GetContentByPath")
            .WithTags("Content")
            .Produces<ContentItemResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapGet(
                "/api/v1/content/{id:guid}/children",
                GetChildrenAsync)
            .WithName("GetContentChildren")
            .WithTags("Content")
            .Produces<ContentItemCollectionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapGet(
                "/api/v1/content/root/children",
                GetRootChildrenAsync)
            .WithName("GetRootContentChildren")
            .WithTags("Content")
            .Produces<ContentItemCollectionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapPost(
                "/api/v1/content",
                CreateAsync)
            .WithName("CreateContent")
            .WithTags("Content")
            .Produces<ContentItemResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapPut(
                "/api/v1/content/{id:guid}",
                UpdateAsync)
            .WithName("UpdateContent")
            .WithTags("Content")
            .Produces<ContentItemResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapPost(
                "/api/v1/content/{id:guid}/values",
                SetValuesAsync)
            .WithName("SetContentValues")
            .WithTags("Content")
            .Produces<ContentItemResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapDelete(
                "/api/v1/content/{id:guid}",
                DeleteAsync)
            .WithName("DeleteContent")
            .WithTags("Content")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapGet(
                "/api/v1/content/{id:guid}/dependencies",
                GetDependenciesAsync)
            .WithName("GetContentDependencies")
            .WithTags("Content")
            .Produces<ContentItemDependencyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    public static async Task<Results<Ok<ContentItemResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid id,
        string? lang,
        int? version,
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentItemService);

        try
        {
            var context =
                CreateContext(
                    lang,
                    version);
            var item =
                await contentItemService.GetItemAsync(
                    new ContentItemId(id),
                    context,
                    cancellationToken);

            if (item == null)
            {
                return TypedResults.Problem(
                    title: "Content item was not found",
                    detail: $"No content item exists with id '{id}'.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            return TypedResults.Ok(
                MapResponse(
                    item,
                    context,
                    $"/api/v1/content/{item.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}"));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.Problem(
                title: "Invalid content lookup request",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public static async Task<Results<Ok<ContentItemResponse>, ProblemHttpResult>> GetByPathAsync(
        string? path,
        string? lang,
        int? version,
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentItemService);

        if (string.IsNullOrWhiteSpace(path))
        {
            return TypedResults.Problem(
                title: "Content path is required",
                detail: "Provide a slash-delimited content path after '/api/v1/content/by-path/'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var normalizedPath =
                new ContentPath("/" + path.Trim('/'));
            var context =
                CreateContext(
                    lang,
                    version);
            var item =
                await contentItemService.GetItemAsync(
                    normalizedPath,
                    context,
                    cancellationToken);

            if (item == null)
            {
                return TypedResults.Problem(
                    title: "Content item was not found",
                    detail: $"No content item exists at path '{normalizedPath}'.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            return TypedResults.Ok(
                MapResponse(
                    item,
                    context,
                    $"/api/v1/content/by-path/{normalizedPath.ToString().TrimStart('/')}?lang={context.Language}&version={context.Version.Value}"));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.Problem(
                title: "Invalid path lookup request",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public static async Task<Results<Ok<ContentItemCollectionResponse>, ProblemHttpResult>> GetChildrenAsync(
        Guid id,
        string? lang,
        int? version,
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentItemService);

        try
        {
            var context =
                CreateContext(
                    lang,
                    version);
            var parent =
                await contentItemService.GetItemAsync(
                    new ContentItemId(id),
                    context,
                    cancellationToken);

            if (parent == null)
            {
                return TypedResults.Problem(
                    title: "Content item was not found",
                    detail: $"No content item exists with id '{id}'.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var children =
                await contentItemService.GetChildItemsAsync(
                    parent.Item.Id,
                    context,
                    cancellationToken);

            return TypedResults.Ok(
                MapCollectionResponse(
                    parent.Item.Id,
                    children,
                    context));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.Problem(
                title: "Invalid content lookup request",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public static async Task<Results<Ok<ContentItemCollectionResponse>, ProblemHttpResult>> GetRootChildrenAsync(
        string? lang,
        int? version,
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentItemService);

        try
        {
            var context =
                CreateContext(
                    lang,
                    version);
            var children =
                await contentItemService.GetChildItemsAsync(
                    null,
                    context,
                    cancellationToken);

            return TypedResults.Ok(
                MapCollectionResponse(
                    null,
                    children,
                    context));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.Problem(
                title: "Invalid content lookup request",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public static async Task<Results<Created<ContentItemResponse>, ProblemHttpResult>> CreateAsync(
        CreateContentItemRequest? request,
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentItemService);

        if (request == null)
        {
            return TypedResults.Problem(
                title: "Content item request is required",
                detail: "Provide a content item payload in the request body.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var itemId =
                new ContentItemId(Guid.NewGuid());
            var item =
                new ContentItemDefinition(
                    itemId,
                    request.Name,
                    new ContentItemKey(request.Key),
                    new TemplateId(request.TemplateId),
                    request.ParentId == null ? null : new ContentItemId(request.ParentId.Value));

            await contentItemService.SaveItemAsync(
                item,
                cancellationToken);

            var context =
                CreateContext(
                    null,
                    null);
            var createdItem =
                await contentItemService.GetItemAsync(
                    itemId,
                    context,
                    cancellationToken);

            if (createdItem == null)
            {
                return TypedResults.Problem(
                    title: "Created content item could not be loaded",
                    detail: $"Content item '{itemId}' was saved but could not be reloaded.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var location =
                $"/api/v1/content/{createdItem.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}";

            return TypedResults.Created(
                location,
                MapResponse(
                    createdItem,
                    context,
                    location));
        }
        catch (InvalidOperationException exception)
        {
            var statusCode =
                exception.Message.Contains(
                    "already exists under parent",
                    StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest;

            return TypedResults.Problem(
                title: statusCode == StatusCodes.Status409Conflict
                    ? "Content item could not be created"
                    : "Invalid content create request",
                detail: exception.Message,
                statusCode: statusCode);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.Problem(
                title: "Invalid content create request",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public static async Task<Results<Ok<ContentItemResponse>, ProblemHttpResult>> UpdateAsync(
        Guid id,
        UpdateContentItemRequest? request,
        string? lang,
        int? version,
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentItemService);

        if (request == null)
        {
            return TypedResults.Problem(
                title: "Content item request is required",
                detail: "Provide a content item payload in the request body.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var itemId =
                new ContentItemId(id);
            var existingItem =
                await contentItemService.GetItemAsync(
                    itemId,
                    CreateContext(
                        lang,
                        version),
                    cancellationToken);

            if (existingItem == null)
            {
                return TypedResults.Problem(
                    title: "Content item was not found",
                    detail: $"No content item exists with id '{id}'.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var updatedItem =
                new ContentItemDefinition(
                    existingItem.Item.Id,
                    request.Name,
                    existingItem.Item.Key,
                    existingItem.Item.TemplateId,
                    existingItem.Item.ParentId);

            await contentItemService.SaveItemAsync(
                updatedItem,
                cancellationToken);

            var context =
                CreateContext(
                    lang,
                    version);
            var refreshedItem =
                await contentItemService.GetItemAsync(
                    itemId,
                    context,
                    cancellationToken);

            if (refreshedItem == null)
            {
                return TypedResults.Problem(
                    title: "Updated content item could not be loaded",
                    detail: $"Content item '{itemId}' was saved but could not be reloaded.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            return TypedResults.Ok(
                MapResponse(
                    refreshedItem,
                    context,
                    $"/api/v1/content/{refreshedItem.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}"));
        }
        catch (InvalidOperationException exception)
        {
            var statusCode =
                exception.Message.Contains(
                    "was not found",
                    StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest;

            return TypedResults.Problem(
                title: statusCode == StatusCodes.Status404NotFound
                    ? "Content item was not found"
                    : "Invalid content update request",
                detail: exception.Message,
                statusCode: statusCode);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.Problem(
                title: "Invalid content update request",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public static async Task<Results<Ok<ContentItemResponse>, ProblemHttpResult>> SetValuesAsync(
        Guid id,
        SetContentFieldValuesRequest? request,
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentItemService);

        if (request == null)
        {
            return TypedResults.Problem(
                title: "Content field value request is required",
                detail: "Provide a field value payload in the request body.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (request.Values == null)
        {
            return TypedResults.Problem(
                title: "Content field values are required",
                detail: "Provide one or more field values keyed by field key.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var itemId =
                new ContentItemId(id);
            var context =
                CreateContext(
                    request.Language,
                    request.Version);

            await contentItemService.SaveFieldValuesAsync(
                itemId,
                context,
                request.Values,
                cancellationToken);

            var item =
                await contentItemService.GetItemAsync(
                    itemId,
                    context,
                    cancellationToken);

            if (item == null)
            {
                return TypedResults.Problem(
                    title: "Content item was not found",
                    detail: $"No content item exists with id '{id}'.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            return TypedResults.Ok(
                MapResponse(
                    item,
                    context,
                    $"/api/v1/content/{item.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}"));
        }
        catch (InvalidOperationException exception)
        {
            var statusCode =
                exception.Message.Contains(
                    "was not found",
                    StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status404NotFound
                    : StatusCodes.Status400BadRequest;

            return TypedResults.Problem(
                title: statusCode == StatusCodes.Status404NotFound
                    ? "Content item was not found"
                    : "Content field values could not be saved",
                detail: exception.Message,
                statusCode: statusCode);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.Problem(
                title: "Invalid content field value request",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> DeleteAsync(
        Guid id,
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentItemService);

        try
        {
            var itemId =
                new ContentItemId(id);
            var existingItem =
                await contentItemService.GetItemAsync(
                    itemId,
                    CreateContext(
                        null,
                        null),
                    cancellationToken);

            if (existingItem == null)
            {
                return TypedResults.Problem(
                    title: "Content item was not found",
                    detail: $"No content item exists with id '{id}'.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            await contentItemService.DeleteItemAsync(
                itemId,
                cancellationToken);

            return TypedResults.NoContent();
        }
        catch (InvalidOperationException exception)
        {
            return TypedResults.Problem(
                title: "Content item could not be deleted",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.Problem(
                title: "Invalid content delete request",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    public static async Task<Results<Ok<ContentItemDependencyResponse>, ProblemHttpResult>> GetDependenciesAsync(
        Guid id,
        string? lang,
        int? version,
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentItemService);

        try
        {
            var context =
                CreateContext(
                    lang,
                    version);
            var itemId =
                new ContentItemId(id);
            var item =
                await contentItemService.GetItemAsync(
                    itemId,
                    context,
                    cancellationToken);

            if (item == null)
            {
                return TypedResults.Problem(
                    title: "Content item was not found",
                    detail: $"No content item exists with id '{id}'.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            var children =
                await contentItemService.GetChildItemsAsync(
                    itemId,
                    context,
                    cancellationToken);

            return TypedResults.Ok(
                new ContentItemDependencyResponse
                {
                    Id = item.Item.Id.Value.ToString(),
                    Path = item.Path.ToString(),
                    CanDelete = children.Count == 0,
                    Summary = new ContentItemDependencySummaryResponse
                    {
                        DirectChildCount = children.Count
                    },
                    Embedded = new ContentItemDependencyEmbeddedResponse
                    {
                        Children = children
                            .OrderBy(child => child.Path.ToString(), StringComparer.Ordinal)
                            .Select(child => MapDependencyChildResponse(child, context))
                            .ToArray()
                    },
                    Links = new ContentItemDependencyLinksResponse
                    {
                        Self = new LinkResponse
                        {
                            Href = $"/api/v1/content/{item.Item.Id.Value}/dependencies?lang={context.Language}&version={context.Version.Value}"
                        },
                        ContentItem = new LinkResponse
                        {
                            Href = $"/api/v1/content/{item.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}"
                        }
                    }
                });
        }
        catch (ArgumentException exception)
        {
            return TypedResults.Problem(
                title: "Invalid content lookup request",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static ContentItemResponse MapResponse(
        ResolvedContentItem item,
        FieldValueResolutionContext context,
        string selfHref)
    {
        var canonicalPath =
            item.Path.ToString();
        var fieldValues =
            item.Fields.ToDictionary(
                pair => pair.Key,
                pair => pair.Value?.Value,
                StringComparer.Ordinal);

        return new ContentItemResponse
        {
            Id = item.Item.Id.Value.ToString(),
            Name = item.Item.Name,
            TemplateId = item.Item.TemplateId.Value.ToString(),
            Path = canonicalPath,
            Language = context.Language.ToString(),
            Version = context.Version.Value,
            Fields = fieldValues,
            Links = new ContentItemLinksResponse
            {
                Self = new LinkResponse
                {
                    Href = selfHref
                },
                Template = new LinkResponse
                {
                    Href = $"/api/v1/templates/{item.Item.TemplateId.Value}"
                },
                Children = new LinkResponse
                {
                    Href = $"/api/v1/content/{item.Item.Id.Value}/children?lang={context.Language}&version={context.Version.Value}"
                },
                Dependencies = new LinkResponse
                {
                    Href = $"/api/v1/content/{item.Item.Id.Value}/dependencies?lang={context.Language}&version={context.Version.Value}"
                },
                SetValues = new LinkResponse
                {
                    Href = $"/api/v1/content/{item.Item.Id.Value}/values"
                },
                Parent = item.Item.ParentId == null
                    ? null
                    : new LinkResponse
                    {
                        Href = $"/api/v1/content/{item.Item.ParentId.Value.Value}?lang={context.Language}&version={context.Version.Value}"
                    }
            }
        };
    }

    private static ContentItemCollectionResponse MapCollectionResponse(
        ContentItemId? parentId,
        IReadOnlyCollection<ResolvedContentItem> children,
        FieldValueResolutionContext context)
    {
        var items =
            children.Select(
                    child =>
                        MapResponse(
                            child,
                            context,
                            $"/api/v1/content/{child.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}"))
                .ToArray();

        return new ContentItemCollectionResponse
        {
            Embedded = new ContentItemCollectionEmbeddedResponse
            {
                Items = items
            },
            Links = new ContentItemCollectionLinksResponse
            {
                Self = new LinkResponse
                {
                    Href = parentId == null
                        ? $"/api/v1/content/root/children?lang={context.Language}&version={context.Version.Value}"
                        : $"/api/v1/content/{parentId.Value.Value}/children?lang={context.Language}&version={context.Version.Value}"
                },
                Parent = parentId == null
                    ? null
                    : new LinkResponse
                    {
                        Href = $"/api/v1/content/{parentId.Value.Value}?lang={context.Language}&version={context.Version.Value}"
                    }
            }
        };
    }

    private static ContentItemDependencyChildResponse MapDependencyChildResponse(
        ResolvedContentItem child,
        FieldValueResolutionContext context)
    {
        return new ContentItemDependencyChildResponse
        {
            Id = child.Item.Id.Value.ToString(),
            Name = child.Item.Name,
            Path = child.Path.ToString(),
            Links = new ContentItemDependencyChildLinksResponse
            {
                Self = new LinkResponse
                {
                    Href = $"/api/v1/content/{child.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}"
                }
            }
        };
    }

    private static FieldValueResolutionContext CreateContext(
        string? lang,
        int? version)
    {
        return new FieldValueResolutionContext(
            new ContentLanguage(lang ?? "en"),
            new ContentVersion(version ?? ContentVersion.First.Value));
    }
}
