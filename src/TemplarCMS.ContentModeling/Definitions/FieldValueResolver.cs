using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Resolves content field values using a field value resolution policy.
/// </summary>
public sealed class FieldValueResolver
    : IFieldValueResolver
{
    private readonly IFieldValueResolutionPolicy _policy;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="FieldValueResolver"/> class.
    /// </summary>
    /// <param name="policy">
    /// The resolution policy.
    /// </param>
    public FieldValueResolver(
        IFieldValueResolutionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        _policy = policy;
    }

    /// <inheritdoc />
    public ContentFieldValue? Resolve(
        FieldDefinition fieldDefinition,
        IReadOnlyCollection<ContentFieldValue> values,
        FieldValueResolutionContext context)
    {
        return _policy.Resolve(
            fieldDefinition,
            values,
            context);
    }
}
