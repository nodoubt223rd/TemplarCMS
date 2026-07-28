namespace TemplarCMS.Domain.Content;

/// <summary>
/// Defines the canonical identifiers for source-controlled starter content items.
/// </summary>
public static class SystemSeedContentIds
{
    public static ContentItemId TemplarRoot { get; } = new(new Guid("0B6AFCDA-BBD1-4CB3-A392-A4078C899F2A"));

    public static ContentItemId ContentRoot { get; } = new(new Guid("93D45FC9-C7A0-48EC-B78D-B8980D4C2DF0"));

    public static ContentItemId Home { get; } = new(new Guid("EF88F020-C20C-47E5-99A8-1E2F6F69506A"));

    public static ContentItemId SystemRoot { get; } = new(new Guid("5F27B5F5-20A7-4445-AEE4-7C955F804084"));

    public static ContentItemId Settings { get; } = new(new Guid("FE75ED5F-EE55-4839-AE36-D1E7AB7E4A3A"));

    public static ContentItemId MediaRoot { get; } = new(new Guid("705D37CB-9A7B-4DDE-AF97-6A0453452DBB"));

    public static ContentItemId Images { get; } = new(new Guid("6784EA0F-53A0-448E-B253-1586E51101F6"));

    public static ContentItemId Files { get; } = new(new Guid("CE0F8D78-C857-483E-9A4B-1F88E31A8A89"));

    public static ContentItemId TemplatesRoot { get; } = new(new Guid("4CF76720-EA11-431C-8DF0-93A057DFAD98"));

    public static ContentItemId StandardTemplateItem { get; } = new(new Guid("74922877-D8DF-466B-93CE-96E5C91D5B7E"));
}
