namespace TemplarCMS.Api.Security;

public sealed class AuthoringSecurityOptions
{
    public const string SectionName = "AuthoringSecurity";

    public bool Enabled { get; set; }

    public string ApiKeyHeaderName { get; set; } = "X-Templar-Api-Key";

    public string? ApiKey { get; set; }
}
