namespace TemplarCMS.Domain.Content;

/// <summary>
/// Represents the supported general link modes.
/// </summary>
public enum GeneralLinkKind
{
    Internal = 0,
    External = 1
}

/// <summary>
/// Represents a structured general link value.
/// </summary>
public sealed class GeneralLinkValue
{
    private GeneralLinkValue(
        GeneralLinkKind kind,
        ContentItemId? itemId,
        Uri? url,
        string? text,
        string? target)
    {
        Kind = kind;
        ItemId = itemId;
        Url = url;
        Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        Target = string.IsNullOrWhiteSpace(target) ? null : target.Trim();
    }

    /// <summary>
    /// Gets the link kind.
    /// </summary>
    public GeneralLinkKind Kind { get; }

    /// <summary>
    /// Gets the internal content item target when present.
    /// </summary>
    public ContentItemId? ItemId { get; }

    /// <summary>
    /// Gets the external target URI when present.
    /// </summary>
    public Uri? Url { get; }

    /// <summary>
    /// Gets the optional link text.
    /// </summary>
    public string? Text { get; }

    /// <summary>
    /// Gets the optional link target.
    /// </summary>
    public string? Target { get; }

    /// <summary>
    /// Creates an internal general link value.
    /// </summary>
    public static GeneralLinkValue Internal(
        ContentItemId itemId,
        string? text = null,
        string? target = null)
    {
        return new GeneralLinkValue(
            GeneralLinkKind.Internal,
            itemId,
            null,
            text,
            target);
    }

    /// <summary>
    /// Creates an external general link value.
    /// </summary>
    public static GeneralLinkValue External(
        Uri url,
        string? text = null,
        string? target = null)
    {
        ArgumentNullException.ThrowIfNull(url);

        return new GeneralLinkValue(
            GeneralLinkKind.External,
            null,
            url,
            text,
            target);
    }
}
