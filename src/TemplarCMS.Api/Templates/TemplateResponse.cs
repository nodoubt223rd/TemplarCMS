using System.Text.Json.Serialization;
using TemplarCMS.Api.Content;

namespace TemplarCMS.Api.Templates;

public sealed class TemplateResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Key { get; init; }

    public required IReadOnlyCollection<TemplateSectionResponse> Sections { get; init; }

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

    public required string SectionId { get; init; }

    public required string SectionName { get; init; }

    public required string SectionKey { get; init; }

    public required int SectionSortOrder { get; init; }
}

public sealed class TemplateFieldCollectionLinksResponse
{
    public required LinkResponse Self { get; init; }

    public required LinkResponse Template { get; init; }

    [JsonPropertyName("create-item")]
    public required LinkResponse CreateItem { get; init; }
}

public sealed class TemplateLinksResponse
{
    public required LinkResponse Self { get; init; }

    public required LinkResponse Fields { get; init; }

    [JsonPropertyName("create-item")]
    public required LinkResponse CreateItem { get; init; }
}
