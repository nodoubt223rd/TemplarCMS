namespace TemplarCMS.Domain.Security;

public sealed record DirectoryRoleDefinition(DirectoryRole Role, string Label, string Description, bool IsAssignable);

public static class DirectoryRoleCatalog
{
    public static bool CanAssignRoles(IReadOnlyList<DirectoryRole>? roles, IReadOnlyList<DirectoryRole>? existingRoles = null) =>
        roles is not null && roles.Distinct().Count() == roles.Count && roles.All(role =>
            Entries.Any(entry => entry.Role == role && (entry.IsAssignable || existingRoles?.Contains(role) == true)));

    public static IReadOnlyList<DirectoryRoleDefinition> Entries { get; } = Array.AsReadOnly<DirectoryRoleDefinition>(
    [
        new(DirectoryRole.Author, "Author", "Creates, updates, and manages structured content items within the lifecycle.", true),
        new(DirectoryRole.Developer, "Developer", "Configures component schemas, headless layouts, and system-level integrations.", true),
        new(DirectoryRole.FormsEditor, "Forms Editor", "Builds, structures, and designs layouts for dynamic data collection forms.", true),
        new(DirectoryRole.TemplarAdmin, "Templar Admin", "Manages root template hierarchies, global settings items, and inheritance baselines.", true),
        new(DirectoryRole.MarketingAutomationEditors, "Marketing Automation Editors", "Configures automated user journeys, triggers, and marketing logic engines.", false)
    ]);
}
