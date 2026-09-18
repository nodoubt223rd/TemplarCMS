using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace TemplarCMS.Api.Tests.Observability;

public sealed class RequestObservabilityIntegrationTests
{
    [Fact]
    public async Task HealthEndpoint_ShouldReturnHealthyAndEchoASafeCorrelationId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const string correlationId = "smoke-test-7f31c";

        await using var factory = new ObservabilityApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-ID", correlationId);
        using var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        Assert.Equal(correlationId, Assert.Single(values));
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReplaceAnUnsafeCorrelationId()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        const string unsafeCorrelationId = "unsafe value";

        await using var factory = new ObservabilityApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-ID", unsafeCorrelationId);
        using var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        var generatedCorrelationId = Assert.Single(values);
        Assert.NotEqual(unsafeCorrelationId, generatedCorrelationId);
        Assert.Matches("^[a-f0-9]{32}$", generatedCorrelationId);
    }

    private sealed class ObservabilityApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        private readonly string _runtimeRootPath;

        public ObservabilityApiFactory()
        {
            _runtimeRootPath =
                Path.Combine(
                    Path.GetTempPath(),
                    "TemplarCMS.Api.Tests",
                    Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_runtimeRootPath);
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration(
                (_, configurationBuilder) =>
                    configurationBuilder.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["OpenApi:Enabled"] = "false",
                            ["Persistence:Provider"] = "Sqlite",
                            ["ConnectionStrings:TemplarCms"] = $"Data Source={Path.Combine(_runtimeRootPath, "templarcms.db")}",
                            ["Templates:TemplatesPath"] = Path.Combine(_runtimeRootPath, "Templates")
                        }));
        }

        public new async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();

            if (Directory.Exists(_runtimeRootPath))
            {
                try
                {
                    Directory.Delete(_runtimeRootPath, recursive: true);
                }
                catch (IOException)
                {
                    // Test-run cleanup is best effort because SQLite may still release a handle.
                }
                catch (UnauthorizedAccessException)
                {
                    // Test-run cleanup is best effort because SQLite may still release a handle.
                }
            }
        }
    }
}
