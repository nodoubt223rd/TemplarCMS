namespace TemplarCMS.Api.Security;

public static class DeliveryApiKeyDefaults
{
    public const string SchemeName = "DeliveryApiKey";
    public const string HeaderName = "X-Templar-Api-Key";
    public const string ClaimType = "templar:delivery";
}
