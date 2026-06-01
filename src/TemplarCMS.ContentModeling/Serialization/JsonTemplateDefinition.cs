using System.Text.Json.Serialization;

namespace TemplarCMS.ContentModeling.Serialization;

/// <summary>
/// Represents a template definition loaded from JSON.
/// </summary>
public sealed class JsonTemplateDefinition
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("baseTemplates")]
    public List<string> BaseTemplates { get; set; } = new();

    [JsonPropertyName("sections")]
    public List<JsonTemplateSectionDefinition> Sections { get; set; } = new();
}
