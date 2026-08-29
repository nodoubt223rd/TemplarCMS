using Microsoft.AspNetCore.Http.HttpResults;
using TemplarCMS.Api;
using Xunit;

namespace TemplarCMS.Api.Tests;

public sealed class ApiRootEndpointsTests
{
    [Theory]
    [InlineData(true, "/openapi/")]
    [InlineData(false, "/api/v1")]
    public void GetLandingPage_ShouldRedirectToTheAvailableDiscoveryPage(
        bool openApiEnabled,
        string expectedLocation)
    {
        var result = ApiRootEndpoints.GetLandingPage(openApiEnabled);

        Assert.Equal(expectedLocation, result.Url);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDiscoveryLinks_WhenOpenApiIsEnabled()
    {
        var result =
            await ApiRootEndpoints.GetAsync(openApiEnabled: true);

        var ok = Assert.IsType<Ok<ApiRootResponse>>(result);
        Assert.NotNull(ok.Value);
        Assert.Equal("TemplarCMS API", ok.Value.Name);
        Assert.Equal("v1", ok.Value.Version);
        Assert.Equal("/api/v1", ok.Value.Links.Self.Href);
        Assert.Equal("/api/v1/templates", ok.Value.Links.Templates.Href);
        Assert.Equal("/api/v1/field-types", ok.Value.Links.FieldTypes.Href);
        Assert.Equal("/api/v1/content/root/branch?lang=en&version=1", ok.Value.Links.ContentRoot.Href);
        Assert.Equal("/api/v1/content/root/children?lang=en&version=1", ok.Value.Links.ContentRootChildren.Href);
        Assert.Equal("/api/v1/templates", ok.Value.Links.CreateTemplate.Href);
        Assert.Equal("/api/v1/content", ok.Value.Links.CreateContent.Href);
        Assert.NotNull(ok.Value.Links.OpenApi);
        Assert.Equal("/openapi/v1.json", ok.Value.Links.OpenApi!.Href);
    }

    [Fact]
    public async Task GetAsync_ShouldOmitOpenApiLink_WhenOpenApiIsDisabled()
    {
        var result =
            await ApiRootEndpoints.GetAsync(openApiEnabled: false);

        var ok = Assert.IsType<Ok<ApiRootResponse>>(result);
        Assert.NotNull(ok.Value);
        Assert.Null(ok.Value.Links.OpenApi);
    }
}
