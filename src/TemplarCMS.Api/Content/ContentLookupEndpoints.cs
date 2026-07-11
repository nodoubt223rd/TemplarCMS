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
                "/api/v1/content/by-path/{**path}",
                GetByPathAsync)
            .WithName("GetContentByPath")
            .WithTags("Content")
            .Produces<ContentItemResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
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
                new FieldValueResolutionContext(
                    new ContentLanguage(lang ?? "en"),
                    new ContentVersion(version ?? ContentVersion.First.Value));
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
                    context));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.Problem(
                title: "Invalid path lookup request",
                detail: exception.Message,
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static ContentItemResponse MapResponse(
        ResolvedContentItem item,
        FieldValueResolutionContext context)
    {
        var canonicalPath =
            item.Path.ToString();
        var routePath =
            canonicalPath.TrimStart('/');
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
                    Href = $"/api/v1/content/by-path/{routePath}?lang={context.Language}&version={context.Version.Value}"
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
}
