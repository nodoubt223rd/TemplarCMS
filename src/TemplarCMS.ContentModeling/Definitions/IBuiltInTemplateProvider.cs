namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Provides the canonical built-in template definitions shipped with TemplarCMS.
/// </summary>
public interface IBuiltInTemplateProvider
{
    /// <summary>
    /// Returns the full set of source-controlled built-in template definitions.
    /// </summary>
    IReadOnlyCollection<TemplateDefinition> GetTemplates();
}
