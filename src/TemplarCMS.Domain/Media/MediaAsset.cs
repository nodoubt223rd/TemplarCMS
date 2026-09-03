using TemplarCMS.Domain.Content;

namespace TemplarCMS.Domain.Media;

/// <summary>Metadata for a raster asset owned by the CMS media library.</summary>
public sealed record MediaAsset(
    Guid Id,
    ContentItemId FolderId,
    string FileName,
    string StoredFileName,
    string ContentType,
    long Length,
    string? AltText,
    string? Title,
    DateTimeOffset CreatedUtc);
