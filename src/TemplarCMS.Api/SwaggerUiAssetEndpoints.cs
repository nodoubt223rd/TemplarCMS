using System.Reflection;
using Microsoft.AspNetCore.StaticFiles;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace TemplarCMS.Api;

/// <summary>Serves Swagger UI assets when the embedded middleware falls through.</summary>
internal static class SwaggerUiAssetEndpoints
{
    private const string ResourcePrefix = "Swashbuckle.AspNetCore.SwaggerUI.node_modules.swagger_ui_dist.";
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();

    public static IEndpointRouteBuilder MapSwaggerUiAssetFallback(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/openapi/{asset}", GetAsync).ExcludeFromDescription();
        return endpoints;
    }

    private static IResult GetAsync(string asset)
    {
        if (asset.Contains('/') || asset.Contains('\\')) return Results.NotFound();
        var assembly = typeof(SwaggerUIOptions).Assembly;
        var stream = assembly.GetManifestResourceStream(ResourcePrefix + asset);
        if (stream == null) return Results.NotFound();
        return Results.File(stream, ContentTypes.TryGetContentType(asset, out var contentType) ? contentType : "application/octet-stream");
    }
}
