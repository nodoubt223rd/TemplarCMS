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
        var configuredTemplatesPath =
            configuration["Templates:TemplatesPath"];
        var templatesPath =
            Path.GetFullPath(
                string.IsNullOrWhiteSpace(configuredTemplatesPath)
                    ? Path.Combine(runtimeDataPath, "Templates")
                    : Path.Combine(environment.ContentRootPath, configuredTemplatesPath));

        services.AddDbContext<TemplarCmsDbContext>(
            options =>
                options.UseSqlite(
                    configuration.GetConnectionString("TemplarCms") ?? defaultConnectionString));

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
        services.AddScoped<IContentPathResolver, ContentPathResolver>();
        services.AddScoped<IContentItemService, ContentItemService>();
        services.AddScoped<IDefaultContentBootstrapper, DefaultContentBootstrapper>();

        services.AddSingleton<IConfigureOptions<JsonTemplateRepositoryOptions>>(
            _ =>
                new ConfigureNamedOptions<JsonTemplateRepositoryOptions>(
                    Options.DefaultName,
                    options => options.TemplatesPath = templatesPath));

        services.AddHostedService<DefaultContentBootstrapHostedService>();

        return services;
    }
}
