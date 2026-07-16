using Microsoft.AspNetCore.Http.HttpResults;
using TemplarCMS.Api;
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

        endpoints.MapGet(
                "/api/v1/content/{id:guid}/branch",
                GetBranchAsync)
            .WithName("GetContentBranch")
            .WithTags("Content")
            .Produces<ContentBranchResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapGet(
                "/api/v1/content/root/branch",
                GetRootBranchAsync)
            .WithName("GetRootContentBranch")
            .WithTags("Content")
            .Produces<ContentBranchResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        endpoints.MapPost(
                "/api/v1/content",
                CreateAsync)
            .WithName("CreateContent")
            .WithTags("Content")
            .Produces<ContentMutationResponse>(StatusCodes.Status201Created)
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
                "/api/v1/content/{id:guid}/rename",
                RenameAsync)
            .WithName("RenameContent")
            .WithTags("Content")
            .Produces<ContentMutationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapPost(
                "/api/v1/content/{id:guid}/move",
                MoveAsync)
            .WithName("MoveContent")
            .WithTags("Content")
            .Produces<ContentMutationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

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
            .Produces<ContentMutationResponse>(StatusCodes.Status200OK)
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
                return ApiProblems.ContentItemNotFound(id);
            }

            return TypedResults.Ok(
                MapResponse(
                    item,
                    context,
                    $"/api/v1/content/{item.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}"));
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidContentLookupRequest(exception.Message);
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
            return ApiProblems.ContentPathRequired();
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
                return ApiProblems.ContentItemNotFound(normalizedPath);
            }

            return TypedResults.Ok(
                MapResponse(
                    item,
                    context,
                    $"/api/v1/content/by-path/{normalizedPath.ToString().TrimStart('/')}?lang={context.Language}&version={context.Version.Value}"));
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidPathLookupRequest(exception.Message);
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
                return ApiProblems.ContentItemNotFound(id);
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
            return ApiProblems.InvalidContentLookupRequest(exception.Message);
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
            return ApiProblems.InvalidContentLookupRequest(exception.Message);
        }
    }

    public static async Task<Results<Ok<ContentBranchResponse>, ProblemHttpResult>> GetBranchAsync(
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
                return ApiProblems.ContentItemNotFound(id);
            }

            var children =
                await contentItemService.GetChildItemsAsync(
                    itemId,
                    context,
                    cancellationToken);

            return TypedResults.Ok(
                MapBranchResponse(
                    item,
                    children,
                    context));
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidContentLookupRequest(exception.Message);
        }
    }

    public static async Task<Results<Ok<ContentBranchResponse>, ProblemHttpResult>> GetRootBranchAsync(
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
                MapRootBranchResponse(
                    children,
                    context));
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidContentLookupRequest(exception.Message);
        }
    }

    public static async Task<Results<Created<ContentMutationResponse>, ProblemHttpResult>> CreateAsync(
        CreateContentItemRequest? request,
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentItemService);

        if (request == null)
        {
            return ApiProblems.ContentItemRequestRequired();
        }

        try
        {
            var itemId =
                new ContentItemId(Guid.NewGuid());
            var item =
                new ContentItemDefinition(
                    itemId,
                    request.Name,
                    ContentItemKey.FromDisplayName(request.Name),
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
                return ApiProblems.CreatedContentItemCouldNotBeLoaded(itemId);
            }

            var location =
                $"/api/v1/content/{createdItem.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}";
            var affectedBranches =
                await LoadAffectedBranchesAsync(
                    contentItemService,
                    context,
                    [("created-under", createdItem.Item.ParentId)],
                    cancellationToken);

            return TypedResults.Created(
                location,
                MapMutationResponse(
                    createdItem,
                    affectedBranches,
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

            return ApiProblems.ContentItemCouldNotBeCreated(exception.Message, statusCode);
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidContentCreateRequest(exception.Message);
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
            return ApiProblems.ContentItemRequestRequired();
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
                return ApiProblems.ContentItemNotFound(id);
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
                return ApiProblems.UpdatedContentItemCouldNotBeLoaded(itemId);
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

            return statusCode == StatusCodes.Status404NotFound
                ? ApiProblems.ContentItemWasNotFound(exception.Message)
                : ApiProblems.InvalidContentUpdateRequest(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidContentUpdateRequest(exception.Message);
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
            return ApiProblems.ContentFieldValueRequestRequired();
        }

        if (request.Values == null)
        {
            return ApiProblems.ContentFieldValuesRequired();
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
                return ApiProblems.ContentItemNotFound(id);
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

            return ApiProblems.ContentFieldValuesCouldNotBeSaved(exception.Message, statusCode);
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidContentFieldValueRequest(exception.Message);
        }
    }

    public static async Task<Results<Ok<ContentMutationResponse>, ProblemHttpResult>> RenameAsync(
        Guid id,
        RenameContentItemRequest? request,
        string? lang,
        int? version,
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentItemService);

        if (request == null)
        {
            return ApiProblems.ContentItemRequestRequired();
        }

        try
        {
            var itemId = new ContentItemId(id);

            await contentItemService.RenameItemAsync(
                itemId,
                request.Name,
                ContentItemKey.FromDisplayName(request.Name),
                cancellationToken);

            var context = CreateContext(lang, version);
            var refreshedItem =
                await contentItemService.GetItemAsync(
                    itemId,
                    context,
                    cancellationToken);

            if (refreshedItem == null)
            {
                return ApiProblems.UpdatedContentItemCouldNotBeLoaded(itemId);
            }

            var affectedBranches =
                await LoadAffectedBranchesAsync(
                    contentItemService,
                    context,
                    [("renamed-under", refreshedItem.Item.ParentId)],
                    cancellationToken);

            return TypedResults.Ok(
                MapMutationResponse(
                    refreshedItem,
                    affectedBranches,
                    context,
                    $"/api/v1/content/{refreshedItem.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}"));
        }
        catch (InvalidOperationException exception)
        {
            var statusCode =
                exception.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status404NotFound
                    : exception.Message.Contains("already exists under parent", StringComparison.OrdinalIgnoreCase)
                        ? StatusCodes.Status409Conflict
                        : StatusCodes.Status400BadRequest;

            return ApiProblems.ContentItemCouldNotBeRenamed(exception.Message, statusCode);
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidContentRenameRequest(exception.Message);
        }
    }

    public static async Task<Results<Ok<ContentMutationResponse>, ProblemHttpResult>> MoveAsync(
        Guid id,
        MoveContentItemRequest? request,
        string? lang,
        int? version,
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contentItemService);

        if (request == null)
        {
            return ApiProblems.ContentItemRequestRequired();
        }

        try
        {
            var itemId = new ContentItemId(id);
            var context = CreateContext(lang, version);
            var existingItem =
                await contentItemService.GetItemAsync(
                    itemId,
                    context,
                    cancellationToken);

            if (existingItem == null)
            {
                return ApiProblems.ContentItemNotFound(id);
            }

            var previousParentId =
                existingItem.Item.ParentId;

            await contentItemService.MoveItemAsync(
                itemId,
                request.ParentId == null ? null : new ContentItemId(request.ParentId.Value),
                cancellationToken);

            var refreshedItem =
                await contentItemService.GetItemAsync(
                    itemId,
                    context,
                    cancellationToken);

            if (refreshedItem == null)
            {
                return ApiProblems.UpdatedContentItemCouldNotBeLoaded(itemId);
            }

            var affectedBranches =
                await LoadAffectedBranchesAsync(
                    contentItemService,
                    context,
                    [
                        ("moved-from", previousParentId),
                        ("moved-to", refreshedItem.Item.ParentId)
                    ],
                    cancellationToken);

            return TypedResults.Ok(
                MapMutationResponse(
                    refreshedItem,
                    affectedBranches,
                    context,
                    $"/api/v1/content/{refreshedItem.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}"));
        }
        catch (InvalidOperationException exception)
        {
            var statusCode =
                exception.Message.Contains("was not found", StringComparison.OrdinalIgnoreCase)
                    ? StatusCodes.Status404NotFound
                    : exception.Message.Contains("already exists under parent", StringComparison.OrdinalIgnoreCase)
                        ? StatusCodes.Status409Conflict
                        : StatusCodes.Status400BadRequest;

            return ApiProblems.ContentItemCouldNotBeMoved(exception.Message, statusCode);
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidContentMoveRequest(exception.Message);
        }
    }

    public static async Task<Results<Ok<ContentMutationResponse>, ProblemHttpResult>> DeleteAsync(
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
                return ApiProblems.ContentItemNotFound(id);
            }

            var context =
                CreateContext(
                    null,
                    null);

            await contentItemService.DeleteItemAsync(
                itemId,
                cancellationToken);

            var affectedBranches =
                await LoadAffectedBranchesAsync(
                    contentItemService,
                    context,
                    [("deleted-from", existingItem.Item.ParentId)],
                    cancellationToken);

            return TypedResults.Ok(
                MapMutationResponse(
                    existingItem,
                    affectedBranches,
                    context,
                    $"/api/v1/content/{existingItem.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}"));
        }
        catch (InvalidOperationException exception)
        {
            return ApiProblems.ContentItemCouldNotBeDeleted(exception.Message);
        }
        catch (ArgumentException exception)
        {
            return ApiProblems.InvalidContentDeleteRequest(exception.Message);
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
                return ApiProblems.ContentItemNotFound(id);
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
            return ApiProblems.InvalidContentLookupRequest(exception.Message);
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
                Rename = new LinkResponse
                {
                    Href = $"/api/v1/content/{item.Item.Id.Value}/rename"
                },
                Move = new LinkResponse
                {
                    Href = $"/api/v1/content/{item.Item.Id.Value}/move"
                },
                Delete = new LinkResponse
                {
                    Href = $"/api/v1/content/{item.Item.Id.Value}"
                },
                Branch = new LinkResponse
                {
                    Href = $"/api/v1/content/{item.Item.Id.Value}/branch?lang={context.Language}&version={context.Version.Value}"
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

    private static ContentBranchResponse MapBranchResponse(
        ResolvedContentItem item,
        IReadOnlyCollection<ResolvedContentItem> children,
        FieldValueResolutionContext context)
    {
        return new ContentBranchResponse
        {
            Item = MapResponse(
                item,
                context,
                $"/api/v1/content/{item.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}"),
            Embedded = new ContentItemBranchEmbeddedResponse
            {
                Children = children
                    .Select(
                        child =>
                            MapResponse(
                                child,
                                context,
                                $"/api/v1/content/{child.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}"))
                    .ToArray()
            },
            Links = new ContentItemBranchLinksResponse
            {
                Self = new LinkResponse
                {
                    Href = $"/api/v1/content/{item.Item.Id.Value}/branch?lang={context.Language}&version={context.Version.Value}"
                },
                Item = new LinkResponse
                {
                    Href = $"/api/v1/content/{item.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}"
                }
            }
        };
    }

    private static ContentBranchResponse MapRootBranchResponse(
        IReadOnlyCollection<ResolvedContentItem> children,
        FieldValueResolutionContext context)
    {
        return new ContentBranchResponse
        {
            Item = null,
            Embedded = new ContentItemBranchEmbeddedResponse
            {
                Children = children
                    .Select(
                        child =>
                            MapResponse(
                                child,
                                context,
                                $"/api/v1/content/{child.Item.Id.Value}?lang={context.Language}&version={context.Version.Value}"))
                    .ToArray()
            },
            Links = new ContentItemBranchLinksResponse
            {
                Self = new LinkResponse
                {
                    Href = $"/api/v1/content/root/branch?lang={context.Language}&version={context.Version.Value}"
                },
                Item = null
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

    private static ContentMutationResponse MapMutationResponse(
        ResolvedContentItem item,
        IReadOnlyCollection<ContentMutationAffectedBranchResponse> affectedBranches,
        FieldValueResolutionContext context,
        string selfHref)
    {
        return new ContentMutationResponse
        {
            Item = MapResponse(
                item,
                context,
                selfHref),
            AffectedBranches = affectedBranches
        };
    }

    private static async Task<IReadOnlyCollection<ContentMutationAffectedBranchResponse>> LoadAffectedBranchesAsync(
        IContentItemService contentItemService,
        FieldValueResolutionContext context,
        IReadOnlyCollection<(string Scope, ContentItemId? ParentId)> branchRequests,
        CancellationToken cancellationToken)
    {
        var uniqueRequests =
            branchRequests
                .DistinctBy(
                    request => request.Scope + "|" + (request.ParentId?.ToString() ?? "<root>"))
                .ToArray();
        var branches =
            new List<ContentMutationAffectedBranchResponse>(uniqueRequests.Length);

        foreach (var request in uniqueRequests)
        {
            if (request.ParentId == null)
            {
                var rootChildren =
                    await contentItemService.GetChildItemsAsync(
                        null,
                        context,
                        cancellationToken);

                branches.Add(
                    new ContentMutationAffectedBranchResponse
                    {
                        Scope = request.Scope,
                        Branch = MapRootBranchResponse(
                            rootChildren,
                            context)
                    });

                continue;
            }

            var parent =
                await contentItemService.GetItemAsync(
                    request.ParentId.Value,
                    context,
                    cancellationToken);

            if (parent == null)
            {
                throw new InvalidOperationException(
                    $"Content item '{request.ParentId.Value}' was saved but its affected branch could not be reloaded.");
            }

            var children =
                await contentItemService.GetChildItemsAsync(
                    request.ParentId.Value,
                    context,
                    cancellationToken);

            branches.Add(
                new ContentMutationAffectedBranchResponse
                {
                    Scope = request.Scope,
                    Branch = MapBranchResponse(
                        parent,
                        children,
                        context)
                });
        }

        return branches;
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
