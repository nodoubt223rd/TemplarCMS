using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TemplarCMS.Api.Content;
using TemplarCMS.Api.Security;
using TemplarCMS.Api.Templates;
using Xunit;

namespace TemplarCMS.Api.Tests.Security;

public sealed class AuthoringSecurityEndpointTests
{
    [Fact]
    public void AuthoringPolicy_ShouldBeRegistered()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddTemplarApiAuthoringSecurity(builder.Configuration);

        using var app = builder.Build();

        var authorizationOptions =
            app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthorizationOptions>>();
        var policy =
            authorizationOptions.Value.GetPolicy(ApiAuthorizationPolicies.AuthorContent);

        Assert.NotNull(policy);
        Assert.Contains(
            policy!.AuthenticationSchemes,
            scheme => string.Equals(scheme, ApiKeyAuthenticationDefaults.SchemeName, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task AuthoringPolicy_ShouldHonorEnabledAndAuthenticationState(
        bool enabled,
        bool isAuthenticated,
        bool expectedSuccess)
    {
        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["AuthoringSecurity:Enabled"] = enabled.ToString()
                    })
                .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddTemplarApiAuthoringSecurity(configuration);

        await using var serviceProvider = services.BuildServiceProvider();
        var authorizationService =
            serviceProvider.GetRequiredService<IAuthorizationService>();
        var principal =
            isAuthenticated
                ? new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, "tester")],
                        "Test"))
                : new ClaimsPrincipal(new ClaimsIdentity());

        var result =
            await authorizationService.AuthorizeAsync(
                principal,
                resource: null,
                ApiAuthorizationPolicies.AuthorContent);

        Assert.Equal(expectedSuccess, result.Succeeded);
    }

    [Theory]
    [InlineData("POST", "/api/v1/content", true)]
    [InlineData("PUT", "/api/v1/content/{id:guid}", true)]
    [InlineData("POST", "/api/v1/content/{id:guid}/rename", true)]
    [InlineData("POST", "/api/v1/content/{id:guid}/move", true)]
    [InlineData("POST", "/api/v1/content/{id:guid}/values", true)]
    [InlineData("DELETE", "/api/v1/content/{id:guid}", true)]
    [InlineData("POST", "/api/v1/templates", true)]
    [InlineData("PUT", "/api/v1/templates/{id:guid}", true)]
    [InlineData("DELETE", "/api/v1/templates/{id:guid}", true)]
    [InlineData("GET", "/api/v1/content/{id:guid}", false)]
    [InlineData("GET", "/api/v1/content/by-path/{**path}", false)]
    [InlineData("GET", "/api/v1/templates", false)]
    [InlineData("GET", "/api/v1/templates/{id:guid}", false)]
    public void EndpointAuthorization_ShouldMatchExpectedRoutes(
        string httpMethod,
        string routePattern,
        bool shouldRequireAuthorization)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddRouting();
        builder.Services.AddAuthorization();

        using var app = builder.Build();

        app.MapContentLookupEndpoints();
        app.MapTemplateEndpoints();
        app.MapFieldTypeEndpoints();

        var routeBuilder =
            (IEndpointRouteBuilder)app;
        var endpoint =
            routeBuilder.DataSources
                .SelectMany(dataSource => dataSource.Endpoints)
                .OfType<RouteEndpoint>()
                .Single(
                    candidate =>
                    {
                        var methods =
                            candidate.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods
                            ?? Array.Empty<string>();

                        return string.Equals(
                                   candidate.RoutePattern.RawText,
                                   routePattern,
                                   StringComparison.Ordinal)
                               && methods.Contains(
                                   httpMethod,
                                   StringComparer.OrdinalIgnoreCase);
                    });
        var authorizeData =
            endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();

        if (shouldRequireAuthorization)
        {
            var requirement = Assert.Single(authorizeData);
            Assert.Equal(ApiAuthorizationPolicies.AuthorContent, requirement.Policy);
            return;
        }

        Assert.Empty(authorizeData);
    }
}
