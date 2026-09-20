namespace TemplarCMS.Domain.Content;

/// <summary>Stable identities used to describe template items.</summary>
public static class SystemTemplateIds
{
    public static TemplateId TemplateFolder { get; } = new(new Guid("1f95b942-d2c6-4e17-b46b-f59c38f16341"));
    public static TemplateId Template { get; } = new(new Guid("a45f8728-e65f-459c-8a64-099c74d9f08e"));
}
