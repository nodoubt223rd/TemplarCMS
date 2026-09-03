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
        await EnsureContentItemIconColumnAsync(dbContext, cancellationToken);
        await EnsureMediaAssetsTableAsync(dbContext, cancellationToken);
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

    private static async Task EnsureContentItemIconColumnAsync(
        TemplarCmsDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var columns = await dbContext.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('ContentItems')")
            .ToListAsync(cancellationToken);

        if (!columns.Contains("Icon", StringComparer.OrdinalIgnoreCase))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE ContentItems ADD COLUMN Icon TEXT NULL",
                cancellationToken);
        }
    }

    private static Task EnsureMediaAssetsTableAsync(
        TemplarCmsDbContext dbContext,
        CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS MediaAssets (
                Id TEXT NOT NULL CONSTRAINT PK_MediaAssets PRIMARY KEY,
                FolderId TEXT NOT NULL,
                FileName TEXT NOT NULL,
                StoredFileName TEXT NOT NULL,
                ContentType TEXT NOT NULL,
                Length INTEGER NOT NULL,
                AltText TEXT NULL,
                Title TEXT NULL,
                CreatedUtc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_MediaAssets_FolderId ON MediaAssets (FolderId);
            """,
            cancellationToken);
}
