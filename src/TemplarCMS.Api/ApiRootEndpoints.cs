using System.Text.Json.Serialization;
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
            () => GetLandingPage(openApiEnabled));

        endpoints.MapGet(
                "/api/v1",
                () => GetAsync(openApiEnabled))
            .WithName("GetApiRoot")
            .WithTags("Discovery")
            .Produces<ApiRootResponse>(StatusCodes.Status200OK);

        return endpoints;
    }

    public static RedirectHttpResult GetLandingPage(bool openApiEnabled)
    {
        return TypedResults.Redirect(openApiEnabled ? "/openapi/" : "/api/v1");
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
