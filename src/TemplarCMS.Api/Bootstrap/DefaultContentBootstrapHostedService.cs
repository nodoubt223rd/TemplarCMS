namespace TemplarCMS.Api.Bootstrap;

/// <summary>Initializes schema and starter content, never credentials.</summary>
public sealed class DefaultContentBootstrapHostedService(CmsRuntimeInitializer initializer) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => initializer.InitializeAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
