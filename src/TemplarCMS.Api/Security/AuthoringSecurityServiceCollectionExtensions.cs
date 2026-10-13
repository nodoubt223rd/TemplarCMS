using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TemplarCMS.Abstractions.Security;
using TemplarCMS.Persistence;
using TemplarCMS.Persistence.Security;

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

        services.AddDataProtection();
        services.AddIdentityCore<CmsIdentityUser>(options =>
        {
            options.Password.RequiredLength = 15;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddEntityFrameworkStores<TemplarCmsDbContext>().AddSignInManager().AddDefaultTokenProviders();
        services.AddScoped<IAdminProvisioningService, AdminProvisioningService>();

        services.AddAuthentication(
                ApiKeyAuthenticationDefaults.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationDefaults.SchemeName,
                _ => { });
        services.AddSingleton<IAuthorizationHandler, AuthoringAccessAuthorizationHandler>();

        services.AddAuthorization(
            options =>
            {
                options.AddPolicy(UserDirectoryEndpoints.Policy, policy => policy
                    .AddAuthenticationSchemes(ApiKeyAuthenticationDefaults.SchemeName)
                    .RequireAuthenticatedUser()
                    .RequireRole(ApiAuthorizationPolicies.AuthorContent));
                options.AddPolicy(
                    ApiAuthorizationPolicies.AuthorContent,
                    policy =>
                    {
                        policy
                            .AddAuthenticationSchemes(ApiKeyAuthenticationDefaults.SchemeName)
                            .AddRequirements(new AuthoringAccessRequirement());
                    });
            });

        return services;
    }
}
