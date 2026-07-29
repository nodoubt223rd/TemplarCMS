using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Supplies the source-controlled built-in template definitions for TemplarCMS.
/// </summary>
public sealed class BuiltInTemplateProvider : IBuiltInTemplateProvider
{
    private static readonly IReadOnlyDictionary<string, string> SystemOwnedSectionMetadata =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [SectionVisibilityMetadata.VisibilityKey] = SectionVisibilityMetadata.SystemValue
        };

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
                    CreateSystemSection(
                        new Guid("55081A71-C336-41F0-B070-F44B84E0D7C0"),
                        "Content",
                        "content",
                        100,
                        [
                            CreateField(
                                new FieldId(new Guid("BE9B2863-EB2D-4D2E-8990-884A87AB6A0B")),
                                "Title",
                                "title",
                                FieldType.SingleLineText,
                                isUnversioned: true),
                            CreateField(
                                new FieldId(new Guid("D315D9AF-F921-4385-BD24-8A97BCE1AFA3")),
                                "Navigation Title",
                                "navigationTitle",
                                FieldType.SingleLineText,
                                isUnversioned: true),
                            CreateField(
                                new FieldId(new Guid("B6A8A944-F09A-4779-83EB-1ABEA205F51C")),
                                "Meta Description",
                                "metaDescription",
                                FieldType.MultiLineText,
                                isUnversioned: true)
                        ]),
                    CreateSystemSection(
                        new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5A001"),
                        "Appearance",
                        "appearance",
                        200,
                        [
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B001")),
                                "__Context Menu",
                                "__contextMenu",
                                FieldType.Droplink,
                                isShared: true),
                            CreateUnversionedField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B002")),
                                "__Display name",
                                "__displayName",
                                FieldType.SingleLineText),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B003")),
                                "__Editor",
                                "__editor",
                                FieldType.File,
                                isShared: true),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B004")),
                                "__Editors",
                                "__editors",
                                FieldType.Multilist,
                                isShared: true),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B005")),
                                "__Hidden",
                                "__hidden",
                                FieldType.Checkbox,
                                isShared: true),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B006")),
                                "__Originator",
                                "__originator",
                                FieldType.Droplink,
                                isShared: true),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B007")),
                                "__Read Only",
                                "__readOnly",
                                FieldType.Checkbox,
                                isShared: true),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B008")),
                                "__Ribbon",
                                "__ribbon",
                                FieldType.Droplink,
                                isShared: true),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B009")),
                                "__Skin",
                                "__skin",
                                FieldType.SingleLineText,
                                isShared: true),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B00A")),
                                "__Sortorder",
                                "__sortorder",
                                FieldType.SingleLineText,
                                isShared: true),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B00B")),
                                "__Style",
                                "__style",
                                FieldType.SingleLineText,
                                isShared: true),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B00C")),
                                "__Subitems Sorting",
                                "__subitemsSorting",
                                FieldType.Droplink,
                                isShared: true)
                        ]),
                    CreateSystemSection(
                        new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5A002"),
                        "Help",
                        "help",
                        300,
                        [
                            CreateUnversionedField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B00D")),
                                "__Help link",
                                "__helpLink",
                                FieldType.GeneralLink),
                            CreateUnversionedField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B00E")),
                                "__Long description",
                                "__longDescription",
                                FieldType.MultiLineText),
                            CreateUnversionedField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B00F")),
                                "__Short description",
                                "__shortDescription",
                                FieldType.SingleLineText)
                        ]),
                    CreateSystemSection(
                        new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5A003"),
                        "Lifetime",
                        "lifetime",
                        400,
                        [
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B010")),
                                "__Hide version",
                                "__hideVersion",
                                FieldType.Checkbox,
                                isShared: true),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B011")),
                                "__Valid from",
                                "__validFrom",
                                FieldType.DateTime,
                                isUnversioned: true),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B012")),
                                "__Valid to",
                                "__validTo",
                                FieldType.DateTime,
                                isUnversioned: true)
                        ]),
                    CreateSystemSection(
                        new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5A004"),
                        "Publishing",
                        "publishing",
                        500,
                        [
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B013")),
                                "__Never publish",
                                "__neverPublish",
                                FieldType.Checkbox,
                                isShared: true),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B014")),
                                "__Publish",
                                "__publish",
                                FieldType.DateTime),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B015")),
                                "__Publishing groups",
                                "__publishingGroups",
                                FieldType.Multilist,
                                isShared: true),
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B016")),
                                "__Unpublish",
                                "__unpublish",
                                FieldType.DateTime)
                        ]),
                    CreateSystemSection(
                        new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5A005"),
                        "Statistics",
                        "statistics",
                        600,
                        [
                            CreateSharedField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B017")),
                                "__Created by",
                                "__createdBy",
                                FieldType.SingleLineText),
                            CreateSharedField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B018")),
                                "__Created",
                                "__created",
                                FieldType.DateTime),
                            CreateSharedField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B019")),
                                "__Revision",
                                "__revision",
                                FieldType.SingleLineText),
                            CreateSharedField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B01A")),
                                "__Updated by",
                                "__updatedBy",
                                FieldType.SingleLineText),
                            CreateSharedField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B01B")),
                                "__Updated",
                                "__updated",
                                FieldType.DateTime)
                        ]),
                    CreateSystemSection(
                        new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5A006"),
                        "Version",
                        "version",
                        700,
                        [
                            CreateField(
                                new FieldId(new Guid("7A1186B2-4A79-4E20-9B31-7F3D94F5B01C")),
                                "__Version Name",
                                "__versionName",
                                FieldType.SingleLineText)
                        ])
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

    private static TemplateSectionDefinition CreateSystemSection(
        Guid id,
        string name,
        string key,
        int sortOrder,
        IReadOnlyCollection<FieldDefinition> fields)
    {
        return new TemplateSectionDefinition(
            id,
            name,
            key,
            sortOrder,
            fields,
            SystemOwnedSectionMetadata);
    }

    private static FieldDefinition CreateField(
        FieldId id,
        string name,
        string key,
        FieldType fieldType,
        bool isShared = false,
        bool isUnversioned = false)
    {
        return new FieldDefinition(
            id,
            name,
            key,
            fieldType,
            isShared,
            isUnversioned);
    }

    private static FieldDefinition CreateSharedField(
        FieldId id,
        string name,
        string key,
        FieldType fieldType)
    {
        return CreateField(
            id,
            name,
            key,
            fieldType,
            isShared: true);
    }

    private static FieldDefinition CreateUnversionedField(
        FieldId id,
        string name,
        string key,
        FieldType fieldType)
    {
        return CreateField(
            id,
            name,
            key,
            fieldType,
            isUnversioned: true);
    }
}
