using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TemplarCMS.Abstractions.Security;
using TemplarCMS.Persistence;
using TemplarCMS.Persistence.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        services.AddDataProtection().SetApplicationName("TemplarCMS")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath(configuration["IdentitySecurity:DataProtectionKeysPath"] ?? "RuntimeData/IdentityKeys")));
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<IAdminIdentityService, AdminIdentityService>();
        services.AddScoped<AccountSessionValidator>();
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-Templar-CSRF";
            options.Cookie.Name = "__Host-TemplarCSRF";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.HttpOnly = true;
            options.Cookie.Path = "/";
        });
        services.AddRateLimiter(options =>
        {
            options.AddPolicy("AdminLogin", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                await context.HttpContext.Response.WriteAsJsonAsync(ApiProblems.Auth(429, "login-rate-limited", "Wait before trying to sign in again."), cancellationToken: ct);
            };
        });
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

        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddCookie(IdentityConstants.ApplicationScheme, options =>
            {
                options.Cookie.Name = "__Host-TemplarAdmin";
                options.Cookie.Path = "/";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = false;
                options.EventsType = typeof(AccountSessionValidator);
            });
        services.AddOptions<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme)
            .Configure<TimeProvider>((options, clock) => options.TimeProvider = clock);
        services.AddSingleton<IAuthorizationHandler, AuthoringAccessAuthorizationHandler>();

        services.AddAuthorization(
            options =>
            {
                options.AddPolicy(ApiAuthorizationPolicies.AccountSession, policy => policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme).RequireAuthenticatedUser());
                options.AddPolicy(UserDirectoryEndpoints.Policy, policy => policy
                    .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme)
                    .RequireAuthenticatedUser().AddRequirements(new AuthoringAccessRequirement()));
                options.AddPolicy(
                    ApiAuthorizationPolicies.AuthorContent,
                    policy =>
                    {
                        policy
                            .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme)
                            .RequireAuthenticatedUser()
                            .AddRequirements(new AuthoringAccessRequirement());
                    });
            });

        return services;
    }
}
