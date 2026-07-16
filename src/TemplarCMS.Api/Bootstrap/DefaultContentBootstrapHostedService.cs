using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TemplarCMS.Application.Bootstrap;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.Persistence;

namespace TemplarCMS.Api.Bootstrap;

/// <summary>
/// Creates local persistence and seeds default CMS structures during app startup.
/// </summary>
public sealed class DefaultContentBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DefaultContentBootstrapHostedService> _logger;

    public DefaultContentBootstrapHostedService(
        IServiceProvider serviceProvider,
        ILogger<DefaultContentBootstrapHostedService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        await using var scope =
            _serviceProvider.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();
        var bootstrapper =
            scope.ServiceProvider.GetRequiredService<IDefaultContentBootstrapper>();
        var templateRepositoryOptions =
            scope.ServiceProvider.GetRequiredService<IOptions<JsonTemplateRepositoryOptions>>();

        EnsureParentDirectoryExists(
            GetSqliteDataSourcePath(
                dbContext.Database.GetConnectionString()));
        Directory.CreateDirectory(templateRepositoryOptions.Value.TemplatesPath);

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await bootstrapper.EnsureInitializedAsync(cancellationToken);

        _logger.LogInformation("Default CMS content bootstrap completed.");
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static string? GetSqliteDataSourcePath(
        string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        var builder = new SqliteConnectionStringBuilder(connectionString);

        return string.IsNullOrWhiteSpace(builder.DataSource)
            ? null
            : Path.GetFullPath(builder.DataSource);
    }

    private static void EnsureParentDirectoryExists(
        string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var directoryPath = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
}
