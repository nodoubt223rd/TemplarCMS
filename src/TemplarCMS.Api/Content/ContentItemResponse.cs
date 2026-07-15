using System.Text.Json.Serialization;

namespace TemplarCMS.Api.Content;

public sealed class CreateContentItemRequest
{
    public required string Name { get; init; }

    public required string Key { get; init; }

    public required Guid TemplateId { get; init; }

    public Guid? ParentId { get; init; }
}

public sealed class UpdateContentItemRequest
{
    public required string Name { get; init; }
}

public sealed class RenameContentItemRequest
{
    public required string Name { get; init; }

    public required string Key { get; init; }
}

public sealed class MoveContentItemRequest
{
    public Guid? ParentId { get; init; }
}

public sealed class SetContentFieldValuesRequest
{
    public required string Language { get; init; }

    public required int Version { get; init; }

    public required IReadOnlyDictionary<string, string?> Values { get; init; }
}

public sealed class ContentItemResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string TemplateId { get; init; }

    public required string Path { get; init; }

    public required string Language { get; init; }

    public required int Version { get; init; }

    public required IReadOnlyDictionary<string, string?> Fields { get; init; }

    [JsonPropertyName("_links")]
    public required ContentItemLinksResponse Links { get; init; }
}

public sealed class ContentItemCollectionResponse
{
    public required ContentItemCollectionEmbeddedResponse Embedded { get; init; }

    [JsonPropertyName("_links")]
    public required ContentItemCollectionLinksResponse Links { get; init; }
}

public sealed class ContentItemCollectionEmbeddedResponse
{
    public required IReadOnlyCollection<ContentItemResponse> Items { get; init; }
}

public sealed class ContentItemCollectionLinksResponse
{
    public required LinkResponse Self { get; init; }

    public LinkResponse? Parent { get; init; }
}

public sealed class ContentBranchResponse
{
    public ContentItemResponse? Item { get; init; }

    public required ContentItemBranchEmbeddedResponse Embedded { get; init; }

    [JsonPropertyName("_links")]
    public required ContentItemBranchLinksResponse Links { get; init; }
}

public sealed class ContentItemBranchEmbeddedResponse
{
    public required IReadOnlyCollection<ContentItemResponse> Children { get; init; }
}

public sealed class ContentItemBranchLinksResponse
{
    public required LinkResponse Self { get; init; }

    public LinkResponse? Item { get; init; }
}

public sealed class ContentMutationResponse
{
    public required ContentItemResponse Item { get; init; }

    public required IReadOnlyCollection<ContentMutationAffectedBranchResponse> AffectedBranches { get; init; }
}

public sealed class ContentMutationAffectedBranchResponse
{
    public required string Scope { get; init; }

    public required ContentBranchResponse Branch { get; init; }
}

public sealed class ContentItemLinksResponse
{
    public required LinkResponse Self { get; init; }

    public required LinkResponse Template { get; init; }

    public required LinkResponse Children { get; init; }

    public required LinkResponse Dependencies { get; init; }

    [JsonPropertyName("set-values")]
    public required LinkResponse SetValues { get; init; }

    public required LinkResponse Rename { get; init; }

    public required LinkResponse Move { get; init; }

    public required LinkResponse Delete { get; init; }

    public required LinkResponse Branch { get; init; }

    public LinkResponse? Parent { get; init; }
}

public sealed class ContentItemDependencyResponse
{
    public required string Id { get; init; }

    public required string Path { get; init; }

    public required bool CanDelete { get; init; }

    public required ContentItemDependencySummaryResponse Summary { get; init; }

    public required ContentItemDependencyEmbeddedResponse Embedded { get; init; }

    [JsonPropertyName("_links")]
    public required ContentItemDependencyLinksResponse Links { get; init; }
}

public sealed class ContentItemDependencySummaryResponse
{
    public required int DirectChildCount { get; init; }
}

public sealed class ContentItemDependencyEmbeddedResponse
{
    public required IReadOnlyCollection<ContentItemDependencyChildResponse> Children { get; init; }
}

public sealed class ContentItemDependencyChildResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Path { get; init; }

    [JsonPropertyName("_links")]
    public required ContentItemDependencyChildLinksResponse Links { get; init; }
}

public sealed class ContentItemDependencyChildLinksResponse
{
    public required LinkResponse Self { get; init; }
}

public sealed class ContentItemDependencyLinksResponse
{
    public required LinkResponse Self { get; init; }

    public required LinkResponse ContentItem { get; init; }
}

public sealed class LinkResponse
{
    public required string Href { get; init; }
}
