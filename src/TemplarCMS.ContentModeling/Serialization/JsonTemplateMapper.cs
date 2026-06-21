using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Serialization
{
    /// <summary>
    /// Maps JSON serialization models into domain content model definitions.
    /// </summary>
    /// <remarks>
    /// This mapper is responsible only for translating JSON DTOs into
    /// domain objects.
    ///
    /// The mapper does not perform inheritance resolution, effective
    /// template generation, duplicate detection, or business validation.
    ///
    /// Base template references remain unresolved during mapping and are
    /// processed later by the repository and catalog pipeline.
    ///
    /// Expected flow:
    ///
    /// JsonTemplateDefinition
    ///         ↓
    /// JsonTemplateMapper
    ///         ↓
    /// TemplateDefinition
    ///         ↓
    /// TemplateValidator
    ///         ↓
    /// TemplateInheritanceResolver
    ///         ↓
    /// EffectiveTemplateBuilder
    /// </remarks>
    public sealed class JsonTemplateMapper : IJsonTemplateMapper
    {
        /// <summary>
        /// Maps a JSON template definition into a domain template definition.
        /// </summary>
        /// <param name="template">
        /// The JSON template definition to map.
        /// </param>
        /// <returns>
        /// A mapped domain template definition.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the supplied template is null.
        /// </exception>
        public TemplateDefinition Map(JsonTemplateDefinition template)
        {
            ArgumentNullException.ThrowIfNull(template);

            var sections =
                MapSections(template.Sections);

            return new TemplateDefinition(
                template.Id,
                GetRequiredValue(template.Name, "name"),
                GetRequiredValue(template.Key, "key"),
                sections: sections);
        }

        private IReadOnlyCollection<TemplateSectionDefinition> MapSections(IReadOnlyCollection<JsonTemplateSectionDefinition>? sections)
        {
            if (sections == null || sections.Count == 0)
            {
                return Array.Empty<TemplateSectionDefinition>();
            }

            return sections
                .Select(MapSection)
                .ToArray();
        }

        private TemplateSectionDefinition MapSection(JsonTemplateSectionDefinition section)
        {
            return new TemplateSectionDefinition(
                section.Id,
                GetRequiredValue(section.Name, "section.name"),
                GetRequiredValue(section.Key, "section.key"),
                section.SortOrder,
                MapFields(section.Fields));
        }

        private IReadOnlyCollection<FieldDefinition> MapFields(IReadOnlyCollection<JsonFieldDefinition>? fields)
        {
            if (fields == null || fields.Count == 0)
            {
                return Array.Empty<FieldDefinition>();
            }

            return fields
                .Select(MapField)
                .ToArray();
        }

        private FieldDefinition MapField(JsonFieldDefinition field)
        {
            return new FieldDefinition(
                new FieldId(field.Id),
                GetRequiredValue(field.Name, "field.name"),
                GetRequiredValue(field.Key, "field.key"),
                ParseFieldType(field.FieldType),
                field.IsShared,
                field.IsUnversioned,
                field.Metadata);
        }

        /// <summary>
        /// Converts a JSON field type identifier into a domain field type.
        /// </summary>
        /// <param name="fieldType">
        /// The JSON field type value.
        /// </param>
        /// <returns>
        /// The corresponding domain field type.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the field type is missing or unsupported.
        /// </exception>
        private static FieldType ParseFieldType(string? fieldType)
        {
            if (string.IsNullOrWhiteSpace(fieldType))
            {
                throw new InvalidOperationException(
                    "Field type is required.");
            }

            return fieldType.Trim().ToLowerInvariant() switch
            {
                "singlelinetext" => FieldType.SingleLineText,
                "multilinetext" => FieldType.MultiLineText,
                "richtext" => FieldType.RichText,
                "checkbox" => FieldType.Checkbox,
                "datetime" => FieldType.DateTime,
                "integer" => FieldType.Integer,
                "decimal" => FieldType.Decimal,
                "droplink" => FieldType.Droplink,
                "multilist" => FieldType.Multilist,
                "image" => FieldType.Image,
                "file" => FieldType.File,
                "json" => FieldType.Json,

                _ => throw new InvalidOperationException(
                    $"Unsupported field type '{fieldType}'.")
            };
        }

        /// <summary>
        /// Retrieves a required string value from a JSON DTO.
        /// </summary>
        /// <param name="value">
        /// The value to validate.
        /// </param>
        /// <param name="propertyName">
        /// The logical property name used in exception messages.
        /// </param>
        /// <returns>
        /// The trimmed string value.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the value is null, empty, or whitespace.
        /// </exception>
        private static string GetRequiredValue(string? value, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Required property '{propertyName}' is missing.");
            }

            return value.Trim();
        }
    }
}
