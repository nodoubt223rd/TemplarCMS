using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemplarCMS.Api.Security;
using TemplarCMS.Api.Templates;
using Xunit;

namespace TemplarCMS.Api.Tests.Security;

public sealed class AuthoringSecurityIntegrationTests
{
    [Fact]
    public async Task SampleHomePage_ShouldRenderTheBootstrappedHomeContent()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var factory = new AuthoringSecurityApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var page = await response.Content.ReadAsStringAsync(cancellationToken);

        Assert.Contains("<title>Home | TemplarCMS</title>", page, StringComparison.Ordinal);
        Assert.Contains("<p>Welcome to Templar CMS.</p>", page, StringComparison.Ordinal);
        Assert.Contains("/author-workspace/", page, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthoringEndpoint_ShouldReturn401_WhenApiKeyIsMissing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var factory = new AuthoringSecurityApiFactory();
        using var client = factory.CreateClient();

        using var response =
            await client.PostAsJsonAsync(
                "/api/v1/templates",
                CreateTemplateRequest(),
                cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem =
            await response.Content.ReadFromJsonAsync<ProblemDetails>(
                cancellationToken);

        Assert.NotNull(problem);
        Assert.Equal("Authoring authentication is required", problem.Title);
        Assert.Equal("/api/problems/authoring-authentication-required", problem.Type);
        Assert.Equal(
            "Provide the configured API key using the 'X-Templar-Api-Key' header.",
            problem.Detail);
        AssertProblemCode(problem, "authoring-authentication-required");
    }

    [Theory]
    [InlineData("POST", "/api/v1/content")]
    [InlineData("PUT", "/api/v1/content/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/v1/content/00000000-0000-0000-0000-000000000001/rename")]
    [InlineData("POST", "/api/v1/content/00000000-0000-0000-0000-000000000001/move")]
    [InlineData("POST", "/api/v1/content/00000000-0000-0000-0000-000000000001/values")]
    [InlineData("DELETE", "/api/v1/content/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/v1/templates")]
    [InlineData("PUT", "/api/v1/templates/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/v1/templates/00000000-0000-0000-0000-000000000001")]
    public async Task AuthoringEndpoints_ShouldReturn401_WhenApiKeyIsMissing(
        string method,
        string requestUri)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var factory = new AuthoringSecurityApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), requestUri);
        using var response = await client.SendAsync(request, cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthoringEndpoint_ShouldReturn401_WhenApiKeyIsInvalid()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var factory = new AuthoringSecurityApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Templar-Api-Key", "invalid-api-key");

        using var response =
            await client.PostAsJsonAsync(
                "/api/v1/templates",
                CreateTemplateRequest(),
                cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var problem =
            await response.Content.ReadFromJsonAsync<ProblemDetails>(
                cancellationToken);

        Assert.NotNull(problem);
        Assert.Equal("Authoring authentication failed", problem.Title);
        Assert.Equal("/api/problems/authoring-authentication-failed", problem.Type);
        AssertProblemCode(problem, "authoring-authentication-failed");
    }

    [Fact]
    public async Task AuthoringEndpoint_ShouldReturn403_WhenAuthorizationFailsAfterAuthentication()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var factory = new AuthoringSecurityApiFactory(forceForbidden: true);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Templar-Api-Key", AuthoringSecurityApiFactory.ValidApiKey);

        using var response =
            await client.PostAsJsonAsync(
                "/api/v1/templates",
                CreateTemplateRequest(),
                cancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var problem =
            await response.Content.ReadFromJsonAsync<ProblemDetails>(
                cancellationToken);

        Assert.NotNull(problem);
        Assert.Equal("Authoring access is forbidden", problem.Title);
        Assert.Equal("/api/problems/authoring-access-forbidden", problem.Type);
        Assert.Equal(
            "The current identity is not allowed to perform this authoring operation.",
            problem.Detail);
        AssertProblemCode(problem, "authoring-access-forbidden");
    }

    [Fact]
    public async Task AuthoringEndpoint_ShouldSucceed_WhenValidApiKeyIsProvided()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var factory = new AuthoringSecurityApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Templar-Api-Key", AuthoringSecurityApiFactory.ValidApiKey);

        var request =
            CreateTemplateRequest();

        using var response =
            await client.PostAsJsonAsync(
                "/api/v1/templates",
                request,
                cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created =
            await response.Content.ReadFromJsonAsync<TemplateResponse>(
                cancellationToken);

        Assert.NotNull(created);
        Assert.Equal(request.Name, created.Name);
        Assert.Equal(request.Key, created.Key);
        Assert.NotEmpty(created.Id);
    }

    private static CreateTemplateRequest CreateTemplateRequest()
    {
        var suffix =
            Guid.NewGuid().ToString("N")[..8];

        return new CreateTemplateRequest
        {
            Name = $"Article Page {suffix}",
            Key = $"article-page-{suffix}",
            Sections = []
        };
    }

    private static void AssertProblemCode(
        ProblemDetails problem,
        string expectedCode)
    {
        Assert.True(problem.Extensions.TryGetValue("code", out var code));
        var actualCode =
            Assert.IsType<JsonElement>(code).GetString();
        Assert.Equal(expectedCode, actualCode);
    }

    private sealed class AuthoringSecurityApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
    {
        public const string ValidApiKey = "templar-test-api-key";

        private readonly string _runtimeRootPath;
        private readonly bool _forceForbidden;

        public AuthoringSecurityApiFactory(
            bool forceForbidden = false)
        {
            _runtimeRootPath =
                Path.Combine(
                    Path.GetTempPath(),
                    "TemplarCMS.Api.Tests",
                    Guid.NewGuid().ToString("N"));
            _forceForbidden = forceForbidden;
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
                            ["AuthoringSecurity:Enabled"] = "true",
                            ["AuthoringSecurity:ApiKeyHeaderName"] = "X-Templar-Api-Key",
                            ["AuthoringSecurity:ApiKey"] = ValidApiKey,
                            ["ConnectionStrings:TemplarCms"] = $"Data Source={Path.Combine(_runtimeRootPath, "templarcms.db")}",
                            ["Templates:TemplatesPath"] = Path.Combine(_runtimeRootPath, "Templates")
                        }));

            if (_forceForbidden)
            {
                builder.ConfigureServices(
                    services =>
                        services.AddSingleton<IAuthorizationHandler, DenyAuthorizationHandler>());
            }
        }

        public new async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();

            if (Directory.Exists(_runtimeRootPath))
            {
                TryDeleteRuntimeRoot();
            }
        }

        private void TryDeleteRuntimeRoot()
        {
            try
            {
                Directory.Delete(_runtimeRootPath, recursive: true);
            }
            catch (IOException)
            {
                // Best-effort cleanup is enough for temp test artifacts.
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup is enough for temp test artifacts.
            }
        }
    }

    private sealed class DenyAuthorizationHandler : IAuthorizationHandler
    {
        public Task HandleAsync(
            AuthorizationHandlerContext context)
        {
            context.Fail();
            return Task.CompletedTask;
        }
    }
}
