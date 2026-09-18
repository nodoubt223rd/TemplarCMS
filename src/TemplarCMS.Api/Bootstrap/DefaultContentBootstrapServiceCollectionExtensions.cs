using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TemplarCMS.Abstractions.Content;
using TemplarCMS.Application.Bootstrap;
using TemplarCMS.Application.Content;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Builders;
using TemplarCMS.ContentModeling.Catalog;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.ContentModeling.Resolvers;
using TemplarCMS.ContentModeling.Serialization;
using TemplarCMS.ContentModeling.Validation;
using TemplarCMS.Persistence;
using TemplarCMS.Persistence.Content;
using TemplarCMS.Persistence.Media;
using TemplarCMS.Abstractions.Media;
using TemplarCMS.Application.Media;

namespace TemplarCMS.Api.Bootstrap;

internal static class DefaultContentBootstrapServiceCollectionExtensions
{
    public static IServiceCollection AddTemplarCmsRuntime(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var runtimeDataPath =
            Path.Combine(
                environment.ContentRootPath,
                "RuntimeData");
        var defaultConnectionString =
            $"Data Source={Path.Combine(runtimeDataPath, "templarcms.db")}";

        services.AddDbContext<TemplarCmsDbContext>(
            options =>
                ConfigureDatabaseProvider(
                    options,
                    configuration["Persistence:Provider"],
                    configuration.GetConnectionString("TemplarCms"),
                    defaultConnectionString));

        services.AddSingleton<IBuiltInTemplateProvider, BuiltInTemplateProvider>();
        services.AddSingleton<IJsonTemplateMapper, JsonTemplateMapper>();
        services.AddSingleton<JsonTemplateRepository>();
        services.AddSingleton<ITemplateRepository>(
            serviceProvider =>
                new BuiltInTemplateRepository(
                    serviceProvider.GetRequiredService<JsonTemplateRepository>(),
                    serviceProvider.GetRequiredService<IBuiltInTemplateProvider>()));
        services.AddSingleton<ITemplateValidator, TemplateValidator>();
        services.AddSingleton<ITemplateInheritanceResolver, TemplateInheritanceResolver>();
        services.AddSingleton<IEffectiveTemplateBuilder, EffectiveTemplateBuilder>();
        services.AddSingleton<IEffectiveTemplateValidator, EffectiveTemplateValidator>();
        services.AddSingleton<IFieldValueResolutionPolicy, ExactMatchFieldValueResolutionPolicy>();
        services.AddSingleton<IFieldValueResolver, FieldValueResolver>();
        services.AddSingleton<ITypedFieldValueConverter, TypedFieldValueConverter>();
        services.AddSingleton<IContentItemResolver, ContentItemResolver>();
        services.AddSingleton<IContentModelCatalog, ContentModelCatalog>();

        services.AddScoped<IContentRepository, EfContentRepository>();
        services.AddScoped<TemplarCMS.Abstractions.Security.IUserDirectoryRepository,
            TemplarCMS.Persistence.Security.EfUserDirectoryRepository>();
        services.AddScoped<IMediaAssetRepository, EfMediaAssetRepository>();
        services.AddScoped<IMediaAssetService, MediaAssetService>();
        services.AddSingleton<IMediaFileStore>(_ => new DirectoryMediaFileStore(Path.Combine(runtimeDataPath, "Media")));
        services.AddScoped<IContentPathResolver, ContentPathResolver>();
        services.AddScoped<IContentItemService, ContentItemService>();
        services.AddScoped<IDefaultContentBootstrapper, DefaultContentBootstrapper>();

        services.AddSingleton<IConfigureOptions<JsonTemplateRepositoryOptions>>(
            _ =>
                new ConfigureNamedOptions<JsonTemplateRepositoryOptions>(
                    Options.DefaultName,
                    options =>
                    {
                        var configuredPath = configuration["Templates:TemplatesPath"];
                        options.TemplatesPath = Path.GetFullPath(
                            string.IsNullOrWhiteSpace(configuredPath)
                                ? Path.Combine(runtimeDataPath, "Templates")
                                : Path.Combine(environment.ContentRootPath, configuredPath));
                    }));

        services.AddHostedService<DefaultContentBootstrapHostedService>();

        return services;
    }

    private static void ConfigureDatabaseProvider(
        DbContextOptionsBuilder options,
        string? configuredProvider,
        string? configuredConnectionString,
        string defaultSqliteConnectionString)
    {
        var provider =
            string.IsNullOrWhiteSpace(configuredProvider)
                ? "Sqlite"
                : configuredProvider.Trim();

        if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            options.UseSqlite(
                configuredConnectionString ?? defaultSqliteConnectionString);
            return;
        }

        if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(configuredConnectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:TemplarCms must be configured when Persistence:Provider is SqlServer.");
            }

            options.UseSqlServer(configuredConnectionString);
            return;
        }

        throw new InvalidOperationException(
            $"Persistence provider '{provider}' is not supported. Use 'Sqlite' or 'SqlServer'.");
    }
}
