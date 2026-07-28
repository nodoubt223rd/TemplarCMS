using System.Text.Json.Serialization;
using TemplarCMS.Api.Content;

namespace TemplarCMS.Api.Templates;

public sealed class TemplateResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Key { get; init; }

    public TemplateBaseTemplateResponse? BaseTemplate { get; init; }

    public required IReadOnlyCollection<TemplateSectionResponse> Sections { get; init; }

    [JsonPropertyName("_links")]
    public required TemplateLinksResponse Links { get; init; }
}

public sealed class TemplateBaseTemplateResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Key { get; init; }

    [JsonPropertyName("_links")]
    public required TemplateBaseTemplateLinksResponse Links { get; init; }
}

public sealed class TemplateBaseTemplateLinksResponse
{
    public required LinkResponse Self { get; init; }
}

public sealed class TemplateCollectionResponse
{
    public required TemplateCollectionEmbeddedResponse Embedded { get; init; }

    [JsonPropertyName("_links")]
    public required TemplateCollectionLinksResponse Links { get; init; }
}

public sealed class CreateTemplateRequest
{
    public required string Name { get; init; }

    public required string Key { get; init; }

    public IReadOnlyCollection<string>? BaseTemplateKeys { get; init; }

    public required IReadOnlyCollection<CreateTemplateSectionRequest> Sections { get; init; }
}

public sealed class CreateTemplateSectionRequest
{
    public required string Name { get; init; }

    public required string Key { get; init; }

    public int SortOrder { get; init; } = 100;

    public required IReadOnlyCollection<CreateTemplateFieldRequest> Fields { get; init; }
}

public sealed class CreateTemplateFieldRequest
{
    public required string Name { get; init; }

    public required string Key { get; init; }

    public required string Type { get; init; }

    public bool IsShared { get; init; }

    public bool IsUnversioned { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

public sealed class TemplateCollectionEmbeddedResponse
{
    public required IReadOnlyCollection<TemplateSummaryResponse> Templates { get; init; }
}

public sealed class TemplateSummaryResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Key { get; init; }

    [JsonPropertyName("_links")]
    public required TemplateLinksResponse Links { get; init; }
}

public sealed class TemplateSectionResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Key { get; init; }

    public required int SortOrder { get; init; }

    public required IReadOnlyCollection<TemplateFieldResponse> Fields { get; init; }
}

public sealed class TemplateFieldResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Key { get; init; }

    public required string Type { get; init; }

    public required bool IsShared { get; init; }

    public required bool IsUnversioned { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

public sealed class TemplateFieldCollectionResponse
{
    public required TemplateFieldCollectionEmbeddedResponse Embedded { get; init; }

    [JsonPropertyName("_links")]
    public required TemplateFieldCollectionLinksResponse Links { get; init; }
}

public sealed class TemplateFieldCollectionEmbeddedResponse
{
    public required IReadOnlyCollection<TemplateFieldItemResponse> Fields { get; init; }
}

public sealed class TemplateFieldItemResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Key { get; init; }

    public required string Type { get; init; }

    public required bool IsShared { get; init; }

    public required bool IsUnversioned { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public required string SectionId { get; init; }

    public required string SectionName { get; init; }

    public required string SectionKey { get; init; }

    public required int SectionSortOrder { get; init; }
}

public sealed class TemplateFieldCollectionLinksResponse
{
    public required LinkResponse Self { get; init; }

    public required LinkResponse Template { get; init; }

    public required LinkResponse Dependencies { get; init; }

    [JsonPropertyName("create-item")]
    public required LinkResponse CreateItem { get; init; }
}

public sealed class TemplateLinksResponse
{
    public required LinkResponse Self { get; init; }

    public required LinkResponse Fields { get; init; }

    public required LinkResponse Dependencies { get; init; }

    [JsonPropertyName("create-item")]
    public required LinkResponse CreateItem { get; init; }
}

public sealed class TemplateCollectionLinksResponse
{
    public required LinkResponse Self { get; init; }
}

public sealed class FieldTypeCollectionResponse
{
    public required FieldTypeCollectionEmbeddedResponse Embedded { get; init; }

    [JsonPropertyName("_links")]
    public required FieldTypeCollectionLinksResponse Links { get; init; }
}

public sealed class FieldTypeCollectionEmbeddedResponse
{
    public required IReadOnlyCollection<FieldTypeResponse> FieldTypes { get; init; }
}

public sealed class FieldTypeCollectionLinksResponse
{
    public required LinkResponse Self { get; init; }
}

public sealed class FieldTypeResponse
{
    public required string Value { get; init; }

    public required string Label { get; init; }

    public required string EditorKind { get; init; }

    public required string InputType { get; init; }

    public string? Placeholder { get; init; }

    public int? Rows { get; init; }

    public string? Step { get; init; }

    public string? HelpText { get; init; }
}

public sealed class TemplateDependencyResponse
{
    public required string TemplateId { get; init; }

    public required string TemplateKey { get; init; }

    public required bool CanDelete { get; init; }

    public required TemplateDependencySummaryResponse Summary { get; init; }

    public required TemplateDependencyEmbeddedResponse Embedded { get; init; }

    [JsonPropertyName("_links")]
    public required TemplateDependencyLinksResponse Links { get; init; }
}

public sealed class TemplateDependencySummaryResponse
{
    public required int DependentTemplateCount { get; init; }

    public required int DependentContentItemCount { get; init; }
}

public sealed class TemplateDependencyEmbeddedResponse
{
    public required IReadOnlyCollection<TemplateDependencyTemplateItemResponse> Templates { get; init; }

    public required IReadOnlyCollection<TemplateDependencyContentItemResponse> ContentItems { get; init; }
}

public sealed class TemplateDependencyTemplateItemResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Key { get; init; }

    [JsonPropertyName("_links")]
    public required TemplateDependencyTemplateItemLinksResponse Links { get; init; }
}

public sealed class TemplateDependencyTemplateItemLinksResponse
{
    public required LinkResponse Self { get; init; }
}

public sealed class TemplateDependencyContentItemResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Path { get; init; }

    [JsonPropertyName("_links")]
    public required TemplateDependencyContentItemLinksResponse Links { get; init; }
}

public sealed class TemplateDependencyContentItemLinksResponse
{
    public required LinkResponse Self { get; init; }
}

public sealed class TemplateDependencyLinksResponse
{
    public required LinkResponse Self { get; init; }

    public required LinkResponse Template { get; init; }
}
