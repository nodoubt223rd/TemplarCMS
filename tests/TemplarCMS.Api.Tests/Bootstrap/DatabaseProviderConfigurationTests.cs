using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using TemplarCMS.ContentModeling.Repositories;
using TemplarCMS.Api.Bootstrap;
using TemplarCMS.Persistence;
using Xunit;

namespace TemplarCMS.Api.Tests.Bootstrap;

public sealed class DatabaseProviderConfigurationTests
{
    [Fact]
    public void Runtime_ShouldUseFinalTemplateDirectoryInsteadOfWritingTestsIntoApplicationData()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Templates:TemplatesPath"] = "RuntimeData/Templates"
        }).Build();
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddTemplarCmsRuntime(configuration, new TestEnvironment());
        var isolatedPath = Path.Combine(Path.GetTempPath(), "TemplarCMS.Tests", Guid.NewGuid().ToString("N"));
        configuration["Templates:TemplatesPath"] = isolatedPath;
        using var provider = services.BuildServiceProvider();

        Assert.Equal(isolatedPath, provider.GetRequiredService<IOptions<JsonTemplateRepositoryOptions>>().Value.TemplatesPath);
    }

    [Fact]
    public void Runtime_ShouldReadProviderAndConnectionFromTheSameFinalConfiguration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Persistence:Provider"] = "SqlServer",
            ["ConnectionStrings:TemplarCms"] = "Server=unused;Database=unused"
        }).Build();
        var services = new ServiceCollection();
        services.AddTemplarCmsRuntime(configuration, new TestEnvironment());

        // WebApplicationFactory applies its configuration overrides after registration.
        configuration["Persistence:Provider"] = "Sqlite";
        configuration["ConnectionStrings:TemplarCms"] = "Data Source=:memory:";
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();

        Assert.True(db.Database.IsSqlite());
        Assert.Equal("Data Source=:memory:", db.Database.GetConnectionString());
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "TemplarCMS.Api.Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
