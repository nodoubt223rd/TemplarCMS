namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Represents a logical field definition within a template schema.
/// </summary>
public sealed class FieldDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldDefinition" /> class.
    /// </summary>
    /// <param name="name">The display name of the field.</param>
    /// <param name="key">The unique field key used for resolution and value storage.</param>
    /// <param name="fieldType">The strongly typed field type.</param>
    /// <param name="isShared">Whether the field value is shared across all languages and versions.</param>
    /// <param name="isUnversioned">Whether the field value varies by language but not by version.</param>
    /// <param name="metadata">Additional field metadata used by editors, validators, and schema generation.</param>
    public FieldDefinition(
        string name,
        string key,
        FieldType fieldType,
        bool isShared = false,
        bool isUnversioned = false,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Field name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Field key is required.", nameof(key));
        }

        Name = name.Trim();
        Key = key.Trim();
        FieldType = fieldType;
        IsShared = isShared;
        IsUnversioned = isUnversioned;
        Metadata = metadata != null
            ? new Dictionary<string, string>(metadata)
            : new Dictionary<string, string>();
    }

    /// <summary>
    /// Gets the display name of the field.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the unique field key used for resolution and value storage.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the strongly typed field type.
    /// </summary>
    public FieldType FieldType { get; }

    /// <summary>
    /// Gets a value indicating whether the field value is shared across all languages and versions.
    /// </summary>
    public bool IsShared { get; }

    /// <summary>
    /// Gets a value indicating whether the field value varies by language but not by version.
    /// </summary>
    public bool IsUnversioned { get; }

    /// <summary>
    /// Gets additional field metadata used by editors, validators, and schema generation.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>
    /// Gets a value indicating whether this field is versioned.
    /// </summary>
    public bool IsVersioned => !IsShared && !IsUnversioned;
}
