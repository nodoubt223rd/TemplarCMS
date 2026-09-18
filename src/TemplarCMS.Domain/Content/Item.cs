namespace TemplarCMS.Domain.Content;

/// <summary>
/// An item is described by a template. Its defining template is independent of
/// any inheritance declared when the item itself represents a template definition.
/// </summary>
public abstract class Item
{
    public abstract string Name { get; }

    public abstract TemplateId TemplateId { get; }
}
