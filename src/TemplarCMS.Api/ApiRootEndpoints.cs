using System.Text.Json.Serialization;
using TemplarCMS.Application.Content;
using TemplarCMS.Domain.Content;
using Microsoft.AspNetCore.Http.HttpResults;
using TemplarCMS.Api.Content;
using TemplarCMS.Api.PublicSite;

namespace TemplarCMS.Api;

public static class ApiRootEndpoints
{
    public static IEndpointRouteBuilder MapApiRootEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool openApiEnabled)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(
            "/",
            GetLandingPageAsync)
            .WithName("GetSampleHomePage")
            .WithTags("Public site")
            .ExcludeFromDescription();

        endpoints.MapGet(
            "/{**contentPath}",
            GetContentPageAsync)
            .WithName("GetPublicContentPage")
            .WithTags("Public site")
            .ExcludeFromDescription();

        endpoints.MapGet(
                "/api/v1",
                () => GetAsync(openApiEnabled))
            .WithName("GetApiRoot")
            .WithTags("Discovery")
            .Produces<ApiRootResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    public static async Task<IResult> GetLandingPageAsync(
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        var home =
            await contentItemService.GetItemAsync(
                SystemSeedContentIds.Home,
                new FieldValueResolutionContext(
                    new ContentLanguage("en"),
                    ContentVersion.First),
                cancellationToken);

        if (home == null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Sample content is initializing",
                detail: "The TemplarCMS starter content is not available yet.");
        }

        var navigationItems =
            await GetNavigationItemsAsync(
                home,
                contentItemService,
                cancellationToken);

        return PublicSitePageRenderer.RenderContentPage(
            home,
            "TemplarCMS sample site",
            navigationItems);
    }

    public static async Task<IResult> GetContentPageAsync(
        string? contentPath,
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contentPath))
        {
            return TypedResults.NotFound();
        }

        var item =
            await contentItemService.GetItemAsync(
                new ContentPath(contentPath),
                new FieldValueResolutionContext(
                    new ContentLanguage("en"),
                    ContentVersion.First),
                cancellationToken);

        if (item == null)
        {
            return PublicSitePageRenderer.RenderNotFoundPage();
        }

        var home =
            await contentItemService.GetItemAsync(
                SystemSeedContentIds.Home,
                new FieldValueResolutionContext(
                    new ContentLanguage("en"),
                    ContentVersion.First),
                cancellationToken);
        var navigationItems =
            await GetNavigationItemsAsync(
                home,
                contentItemService,
                cancellationToken);

        return PublicSitePageRenderer.RenderContentPage(item, "TemplarCMS", navigationItems);
    }

    private static async Task<IReadOnlyCollection<ResolvedContentItem>> GetNavigationItemsAsync(
        ResolvedContentItem? home,
        IContentItemService contentItemService,
        CancellationToken cancellationToken)
    {
        if (home == null)
        {
            return Array.Empty<ResolvedContentItem>();
        }

        return await contentItemService.GetChildItemsAsync(
            home.Item.Id,
            new FieldValueResolutionContext(
                new ContentLanguage("en"),
                ContentVersion.First),
            cancellationToken);
    }

    public static Task<Ok<ApiRootResponse>> GetAsync(bool openApiEnabled)
    {
        return Task.FromResult(
            TypedResults.Ok(
                new ApiRootResponse
                {
                    Name = "TemplarCMS API",
                    Version = "v1",
                    Links = new ApiRootLinksResponse
                    {
                        Self = new LinkResponse
                        {
                            Href = "/api/v1"
                        },
                        Templates = new LinkResponse
                        {
                            Href = "/api/v1/templates"
                        },
                        FieldTypes = new LinkResponse
                        {
                            Href = "/api/v1/field-types"
                        },
                        ContentRoot = new LinkResponse
                        {
                            Href = "/api/v1/content/root/branch?lang=en&version=1"
                        },
                        ContentRootChildren = new LinkResponse
                        {
                            Href = "/api/v1/content/root/children?lang=en&version=1"
                        },
                        CreateTemplate = new LinkResponse
                        {
                            Href = "/api/v1/templates"
                        },
                        CreateContent = new LinkResponse
                        {
                            Href = "/api/v1/content"
                        },
                        OpenApi = openApiEnabled
                            ? new LinkResponse
                            {
                                Href = "/openapi/v1.json"
                            }
                            : null
                    }
                }));
    }
}

public sealed class ApiRootResponse
{
    public required string Name { get; init; }

    public required string Version { get; init; }

    [JsonPropertyName("_links")]
    public required ApiRootLinksResponse Links { get; init; }
}

public sealed class ApiRootLinksResponse
{
    public required LinkResponse Self { get; init; }

    public required LinkResponse Templates { get; init; }

    public required LinkResponse FieldTypes { get; init; }

    public required LinkResponse ContentRoot { get; init; }

    public required LinkResponse ContentRootChildren { get; init; }

    public required LinkResponse CreateTemplate { get; init; }

    public required LinkResponse CreateContent { get; init; }

    public LinkResponse? OpenApi { get; init; }
}
