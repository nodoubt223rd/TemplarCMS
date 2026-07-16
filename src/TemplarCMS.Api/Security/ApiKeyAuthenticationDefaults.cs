namespace TemplarCMS.Api.Security;

public static class ApiKeyAuthenticationDefaults
{
    public const string SchemeName = "TemplarApiKey";
    public const string AuthenticationType = "ApiKey";
    public const string SubjectClaimType = "templar-api-key";
}
