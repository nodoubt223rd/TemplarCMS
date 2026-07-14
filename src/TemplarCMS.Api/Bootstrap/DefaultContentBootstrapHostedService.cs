using Microsoft.EntityFrameworkCore;
using TemplarCMS.Application.Bootstrap;
using TemplarCMS.Persistence;

namespace TemplarCMS.Api.Bootstrap;

/// <summary>
/// Creates local persistence and seeds default CMS structures during app startup.
/// </summary>
public sealed class DefaultContentBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DefaultContentBootstrapHostedService> _logger;

    public DefaultContentBootstrapHostedService(
        IServiceProvider serviceProvider,
        IWebHostEnvironment environment,
        ILogger<DefaultContentBootstrapHostedService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(
            Path.Combine(
                _environment.ContentRootPath,
                "App_Data"));

        await using var scope =
            _serviceProvider.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();
        var bootstrapper =
            scope.ServiceProvider.GetRequiredService<IDefaultContentBootstrapper>();

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await bootstrapper.EnsureInitializedAsync(cancellationToken);

        _logger.LogInformation("Default CMS content bootstrap completed.");
    }

    public Task StopAsync(
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
