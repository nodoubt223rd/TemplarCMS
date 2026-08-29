using System.Text.Json.Serialization;
using System.Net;
using TemplarCMS.Application.Content;
using TemplarCMS.Domain.Content;
using Microsoft.AspNetCore.Http.HttpResults;
using TemplarCMS.Api.Content;

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

        return RenderContentPage(home, "TemplarCMS sample site", navigationItems);
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
            return RenderNotFoundPage();
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

        return RenderContentPage(item, "TemplarCMS", navigationItems);
    }

    private static IResult RenderContentPage(
        ResolvedContentItem item,
        string eyebrow,
        IReadOnlyCollection<ResolvedContentItem> navigationItems)
    {
        var title = GetFieldValue(item, "title") ?? item.Item.Name;
        var navigationTitle = GetFieldValue(item, "navigationTitle") ?? title;
        var metaDescription = GetFieldValue(item, "metaDescription") ?? string.Empty;
        var body = GetFieldValue(item, "body") ?? string.Empty;
        var navigation = RenderNavigation(navigationItems);

        // Rich text is authored HTML; the other values are encoded before being inserted into the page shell.
        var page = $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="description" content="{{WebUtility.HtmlEncode(metaDescription)}}">
              <title>{{WebUtility.HtmlEncode(title)}} | TemplarCMS</title>
              <style>
                :root { color-scheme: light; font-family: Georgia, 'Times New Roman', serif; color: #1d3027; background: #f5f2e8; }
                body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: radial-gradient(circle at top left, #d7e8d1, transparent 42%), #f5f2e8; }
                main { width: min(42rem, calc(100% - 3rem)); padding: 4rem 0; }
                header { border-bottom: 1px solid #a4b9a5; padding-bottom: 1.5rem; }
                nav { border-bottom: 1px solid #d4ded1; padding: 1rem 0; }
                nav ul { display: flex; flex-wrap: wrap; gap: .5rem 1.25rem; list-style: none; margin: 0; padding: 0; }
                nav a { text-decoration-thickness: 1px; text-underline-offset: .2em; }
                .eyebrow { color: #55705d; font-family: ui-monospace, monospace; font-size: .75rem; letter-spacing: .14em; text-transform: uppercase; }
                h1 { font-size: clamp(2.5rem, 8vw, 5rem); font-weight: 400; letter-spacing: -.05em; line-height: .95; margin: .5rem 0 0; }
                article { font-size: 1.2rem; line-height: 1.7; padding: 2rem 0; }
                footer { color: #55705d; font-family: ui-monospace, monospace; font-size: .8rem; }
                a { color: inherit; }
              </style>
            </head>
            <body>
              <main>
                <header>
                  <div class="eyebrow">{{WebUtility.HtmlEncode(eyebrow)}}</div>
                  <h1>{{WebUtility.HtmlEncode(navigationTitle)}}</h1>
                </header>
                {{navigation}}
                <article>{{body}}</article>
                <footer><a href="/author-workspace/">Author workspace</a> <span aria-hidden="true">/</span> <a href="/openapi/">API documentation</a></footer>
              </main>
            </body>
            </html>
            """;

        return TypedResults.Content(page, "text/html; charset=utf-8");
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

    private static string RenderNavigation(
        IReadOnlyCollection<ResolvedContentItem> navigationItems)
    {
        if (navigationItems.Count == 0)
        {
            return string.Empty;
        }

        var links = string.Join(
            Environment.NewLine,
            navigationItems.Select(
                item =>
                {
                    var label =
                        GetFieldValue(item, "navigationTitle")
                        ?? GetFieldValue(item, "title")
                        ?? item.Item.Name;

                    return $"<li><a href=\"{WebUtility.HtmlEncode(item.Path.Value)}\">{WebUtility.HtmlEncode(label)}</a></li>";
                }));

        return $$"""
            <nav aria-label="Site navigation">
              <ul>
                {{links}}
              </ul>
            </nav>
            """;
    }

    private static IResult RenderNotFoundPage()
    {
        const string page = """
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Page not found | TemplarCMS</title>
            </head>
            <body>
              <main>
                <h1>Page not found</h1>
                <p>The requested content does not exist.</p>
                <p><a href="/">Return to the sample home page</a></p>
              </main>
            </body>
            </html>
            """;

        return TypedResults.Content(
            page,
            "text/html; charset=utf-8",
            statusCode: StatusCodes.Status404NotFound);
    }

    private static string? GetFieldValue(ResolvedContentItem item, string fieldKey)
    {
        return item.Fields.TryGetValue(fieldKey, out var fieldValue)
            ? fieldValue?.Value
            : null;
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
