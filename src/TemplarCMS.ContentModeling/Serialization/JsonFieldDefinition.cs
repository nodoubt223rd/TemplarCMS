using System.Text.Json.Serialization;

namespace TemplarCMS.ContentModeling.Serialization;

/// <summary>
/// Represents a field definition loaded from JSON.
/// </summary>
public sealed class JsonFieldDefinition
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("fieldType")]
    public string? FieldType { get; set; }

    [JsonPropertyName("isShared")]
    public bool IsShared { get; set; }

    [JsonPropertyName("isUnversioned")]
    public bool IsUnversioned { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}
