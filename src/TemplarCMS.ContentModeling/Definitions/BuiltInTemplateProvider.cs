using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Supplies the source-controlled built-in template definitions for TemplarCMS.
/// </summary>
public sealed class BuiltInTemplateProvider : IBuiltInTemplateProvider
{
    private static readonly IReadOnlyCollection<TemplateDefinition> Templates = CreateTemplates();

    /// <inheritdoc />
    public IReadOnlyCollection<TemplateDefinition> GetTemplates()
    {
        return Templates;
    }

    private static IReadOnlyCollection<TemplateDefinition> CreateTemplates()
    {
        var standardTemplate =
            new TemplateDefinition(
                new TemplateId(new Guid("95071327-4AAB-4827-9641-1C45EF6A1D37")),
                "Standard",
                BuiltInTemplateKeys.Standard,
                sections:
                [
                    new TemplateSectionDefinition(
                        new Guid("55081A71-C336-41F0-B070-F44B84E0D7C0"),
                        "Content",
                        "content",
                        100,
                        [
                            new FieldDefinition(
                                new FieldId(new Guid("BE9B2863-EB2D-4D2E-8990-884A87AB6A0B")),
                                "Title",
                                "title",
                                FieldType.SingleLineText,
                                isUnversioned: true),
                            new FieldDefinition(
                                new FieldId(new Guid("D315D9AF-F921-4385-BD24-8A97BCE1AFA3")),
                                "Navigation Title",
                                "navigationTitle",
                                FieldType.SingleLineText,
                                isUnversioned: true),
                            new FieldDefinition(
                                new FieldId(new Guid("B6A8A944-F09A-4779-83EB-1ABEA205F51C")),
                                "Meta Description",
                                "metaDescription",
                                FieldType.MultiLineText,
                                isUnversioned: true)
                        ],
                        new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            [SectionVisibilityMetadata.VisibilityKey] = SectionVisibilityMetadata.SystemValue
                        })
                ]);

        var folderTemplate =
            new TemplateDefinition(
                new TemplateId(new Guid("6991D76D-6475-4A2B-B04F-D16E9E4AAE9F")),
                "Folder",
                BuiltInTemplateKeys.Folder,
                standardTemplate,
                []);

        var itemTemplate =
            new TemplateDefinition(
                new TemplateId(new Guid("562BA716-A878-45E5-9BA7-397F46BA7B1D")),
                "Item",
                BuiltInTemplateKeys.Item,
                standardTemplate,
                [
                    new TemplateSectionDefinition(
                        new Guid("6A1AE5E6-BB4E-4EB9-90BE-FA03C50D9C6D"),
                        "Content",
                        "content",
                        100,
                        [
                            new FieldDefinition(
                                new FieldId(new Guid("EA54795D-4FBE-477B-A2CC-F8DA57485729")),
                                "Body",
                                "body",
                                FieldType.RichText)
                        ])
                ]);

        return [standardTemplate, folderTemplate, itemTemplate];
    }
}
