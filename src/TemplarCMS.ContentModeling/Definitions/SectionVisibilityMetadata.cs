namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Defines section metadata conventions used to control authoring visibility.
/// </summary>
public static class SectionVisibilityMetadata
{
    /// <summary>
    /// Gets the metadata key used to declare section visibility.
    /// </summary>
    public const string VisibilityKey = "templar.visibility";

    /// <summary>
    /// Gets the metadata value used for system-owned sections that should stay hidden from standard authoring views.
    /// </summary>
    public const string SystemValue = "system";

    /// <summary>
    /// Returns whether the supplied section metadata marks the section as system-owned.
    /// </summary>
    public static bool IsSystemOwned(
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata == null)
        {
            return false;
        }

        return metadata.TryGetValue(VisibilityKey, out var visibility)
            && string.Equals(
                visibility?.Trim(),
                SystemValue,
                StringComparison.OrdinalIgnoreCase);
    }
}
