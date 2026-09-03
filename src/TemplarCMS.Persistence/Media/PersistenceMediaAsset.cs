namespace TemplarCMS.Persistence.Media;

public sealed class PersistenceMediaAsset
{
    public Guid Id { get; set; }
    public Guid FolderId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Length { get; set; }
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
}
