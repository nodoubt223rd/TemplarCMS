using Microsoft.AspNetCore.Mvc;
using TemplarCMS.Api.Security;
using TemplarCMS.Application.Media;
using TemplarCMS.Domain.Content;
using TemplarCMS.Domain.Media;

namespace TemplarCMS.Api.Media;

public static class MediaEndpoints
{
    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/media/assets", GetAllAsync).WithName("GetMediaAssets").WithTags("Media").Produces<MediaAssetCollectionResponse>();
        endpoints.MapGet("/api/v1/media/assets/{id:guid}/content", GetContentAsync).WithName("GetMediaAssetContent").WithTags("Media");
        endpoints.MapPost("/api/v1/media/assets", UploadAsync).WithName("UploadMediaAsset").WithTags("Media").Accepts<IFormFile>("multipart/form-data").RequireAuthorization(ApiAuthorizationPolicies.AuthorContent);
        return endpoints;
    }

    public static async Task<IResult> GetAllAsync([FromServices] IMediaAssetService service, CancellationToken cancellationToken) => Results.Ok(new MediaAssetCollectionResponse { Assets = (await service.GetAllAsync(cancellationToken)).Select(Map).ToArray() });
    public static async Task<IResult> GetContentAsync(Guid id, [FromServices] IMediaAssetService service, CancellationToken cancellationToken)
    {
        var asset = await service.GetAsync(id, cancellationToken);
        if (asset == null) return Results.NotFound();
        var stream = await service.OpenReadAsync(asset, cancellationToken);
        return stream == null ? Results.NotFound() : Results.File(stream, asset.ContentType, enableRangeProcessing: true);
    }
    public static async Task<IResult> UploadAsync(IFormFile? file, Guid? folderId, string? altText, string? title, [FromServices] IMediaAssetService service, CancellationToken cancellationToken)
    {
        if (file == null || folderId == null) return Results.BadRequest(new { detail = "Provide an image file and media folder id." });
        try { await using var content = file.OpenReadStream(); var asset = await service.CreateAsync(new ContentItemId(folderId.Value), file.FileName, file.ContentType, content, file.Length, altText, title, cancellationToken); return Results.Created($"/api/v1/media/assets/{asset.Id}", Map(asset)); }
        catch (ArgumentException exception) { return Results.BadRequest(new { detail = exception.Message }); }
    }
    private static MediaAssetResponse Map(MediaAsset asset) => new() { Id = asset.Id, FolderId = asset.FolderId.Value, FileName = asset.FileName, ContentType = asset.ContentType, Length = asset.Length, AltText = asset.AltText, Title = asset.Title, CreatedUtc = asset.CreatedUtc, ContentUrl = $"/api/v1/media/assets/{asset.Id}/content" };
}

public sealed class MediaAssetCollectionResponse { public IReadOnlyCollection<MediaAssetResponse> Assets { get; init; } = []; }
public sealed class MediaAssetResponse { public Guid Id { get; init; } public Guid FolderId { get; init; } public string FileName { get; init; } = string.Empty; public string ContentType { get; init; } = string.Empty; public long Length { get; init; } public string? AltText { get; init; } public string? Title { get; init; } public DateTimeOffset CreatedUtc { get; init; } public string ContentUrl { get; init; } = string.Empty; }
