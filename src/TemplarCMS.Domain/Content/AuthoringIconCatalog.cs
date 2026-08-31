namespace TemplarCMS.Domain.Content;

/// <summary>Defines the icon keys shipped by the CMS authoring experience.</summary>
public static class AuthoringIconCatalog
{
    private static readonly HashSet<string> IconKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "house", "file", "folder", "article", "image", "tag", "user", "users",
        "mail", "star", "shield", "calendar", "bookmark", "globe", "layers", "grid",
        "layout", "video", "code", "chart", "link", "pin", "bell", "rocket", "settings"
    };

    public static string? Normalize(string? icon)
    {
        if (string.IsNullOrWhiteSpace(icon)) return null;

        var normalized = icon.Trim().ToLowerInvariant();
        if (!IconKeys.Contains(normalized))
        {
            throw new ArgumentException($"'{icon}' is not an available CMS icon.", nameof(icon));
        }

        return normalized;
    }
}
