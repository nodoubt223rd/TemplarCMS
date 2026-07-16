using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace TemplarCMS.Api.Security;

public static class AuthoringSecurityServiceCollectionExtensions
{
    public static IServiceCollection AddTemplarApiAuthoringSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AuthoringSecurityOptions>(
            configuration.GetSection(AuthoringSecurityOptions.SectionName));

        services.AddAuthentication(
                ApiKeyAuthenticationDefaults.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationDefaults.SchemeName,
                _ => { });

        services.AddAuthorization(
            options =>
            {
                options.AddPolicy(
                    ApiAuthorizationPolicies.AuthorContent,
                    policy =>
                    {
                        policy
                            .AddAuthenticationSchemes(ApiKeyAuthenticationDefaults.SchemeName)
                            .RequireAuthenticatedUser();
                    });
            });

        return services;
    }
}
