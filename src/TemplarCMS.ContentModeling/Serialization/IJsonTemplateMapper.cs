using TemplarCMS.ContentModeling.Definitions;

namespace TemplarCMS.ContentModeling.Serialization;

/// <summary>
/// Maps JSON template DTOs into domain template definitions.
/// </summary>
public interface IJsonTemplateMapper
{
    /// <summary>
    /// Maps a JSON template definition into a domain template definition.
    /// </summary>
    /// <param name="template">
    /// The JSON template definition.
    /// </param>
    /// <returns>
    /// The mapped domain template.
    /// </returns>
    TemplateDefinition Map(
        JsonTemplateDefinition template);
}
