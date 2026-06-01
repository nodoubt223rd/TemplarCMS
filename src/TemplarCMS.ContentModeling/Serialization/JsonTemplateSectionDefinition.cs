using System.Text.Json.Serialization;

namespace TemplarCMS.ContentModeling.Serialization;

/// <summary>
/// Represents a template section loaded from JSON.
/// </summary>
public sealed class JsonTemplateSectionDefinition
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    [JsonPropertyName("fields")]
    public List<JsonFieldDefinition> Fields { get; set; } = new();
}
