using System.Text.Json.Serialization;

namespace TemplarCMS.Api.Content;

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

public sealed class ContentItemLinksResponse
{
    public required LinkResponse Self { get; init; }

    public required LinkResponse Template { get; init; }

    public required LinkResponse Children { get; init; }

    [JsonPropertyName("set-values")]
    public required LinkResponse SetValues { get; init; }

    public LinkResponse? Parent { get; init; }
}

public sealed class LinkResponse
{
    public required string Href { get; init; }
}
