namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Defines field metadata conventions used to control authoring visibility.
/// </summary>
public static class FieldVisibilityMetadata
{
    /// <summary>
    /// Gets the metadata key used to declare field visibility.
    /// </summary>
    public const string VisibilityKey = "templar.visibility";

    /// <summary>
    /// Gets the metadata value used for system-owned fields that should stay hidden from standard authoring views.
    /// </summary>
    public const string SystemValue = "system";

    /// <summary>
    /// Returns whether the supplied field metadata marks the field as system-owned.
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
                visibility,
                SystemValue,
                StringComparison.OrdinalIgnoreCase);
    }
}
