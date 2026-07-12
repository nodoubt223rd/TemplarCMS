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

    private static FieldValueResolutionContext CreateContext(
        string? lang,
        int? version)
    {
        return new FieldValueResolutionContext(
            new ContentLanguage(lang ?? "en"),
            new ContentVersion(version ?? ContentVersion.First.Value));
    }
}
