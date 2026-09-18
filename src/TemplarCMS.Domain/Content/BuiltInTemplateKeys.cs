namespace TemplarCMS.Domain.Content;

/// <summary>
/// Defines the canonical built-in template keys reserved by the system.
/// </summary>
public static class BuiltInTemplateKeys
{
    /// <summary>
    /// Gets the baseline template inherited by built-in authored templates.
    /// </summary>
    public static TemplateKey Standard { get; } = new("standard");

    /// <summary>
    /// Gets the built-in folder template key.
    /// </summary>
    public static TemplateKey Folder { get; } = new("folder");

    /// <summary>
    /// Gets the template describing template definition items.
    /// </summary>
    public static TemplateKey Template { get; } = new("template");

    /// <summary>
    /// Gets all canonical built-in template keys.
    /// </summary>
    public static IReadOnlyList<TemplateKey> All { get; } =
        [Standard, Folder, Template, new("advanced"), new("appearance"), new("help"),
         new("lifetime"), new("publishing"), new("statistics"), new("tasks"), new("version")];
}
