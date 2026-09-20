namespace TemplarCMS.Domain.Content;

public readonly record struct TemplateFolderId
{
    [System.Text.Json.Serialization.JsonConstructor]
    public TemplateFolderId(Guid value)
    {
        if (value == Guid.Empty) throw new ArgumentException("Template folder ID is required.", nameof(value));
        Value = value;
    }
    public Guid Value { get; }
    public override string ToString() => Value.ToString();
}
