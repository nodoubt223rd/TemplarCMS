using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemplarCMS.Api.Security;
using TemplarCMS.Api.Tests.Support;
using Xunit;

namespace TemplarCMS.Api.Tests.Security;

public sealed class DeliveryKeyRotationTests
{
    [Fact]
    public async Task ConfigurationIsFrozenAtStartupEvenBeforeTheFirstDeliveryRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new DeliveryApiFactory();
        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        var replacement = DeliveryKeyGenerator.Generate();
        configuration["DeliverySecurity:KeyDigests:site-A"] = replacement.Sha256Digest;
        ((IConfigurationRoot)configuration).Reload();
        using var client = factory.CreateAnonymousClient();
        DeliveryAuthorizationTests.SetKey(client, factory.SiteKey.Key);
        using var beforeRestart = await client.GetAsync("/api/v1/content/root/children", ct);
        Assert.Equal(HttpStatusCode.OK, beforeRestart.StatusCode);
        DeliveryAuthorizationTests.SetKey(client, replacement.Key);
        using var notYet = await client.GetAsync("/api/v1/content/root/children", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, notYet.StatusCode);
    }

    [Fact]
    public async Task RestartRotatesOneSiteWithoutChangingAnotherAndEmptyConfigurationRevokesAll()
    {
        var ct = TestContext.Current.CancellationToken;
        var old = DeliveryKeyGenerator.Generate();
        var next = DeliveryKeyGenerator.Generate();
        var other = DeliveryKeyGenerator.Generate();
        await using var factory = new DeliveryApiFactory { Overrides = new()
        {
            ["DeliverySecurity:KeyDigests:site-A"] = old.Sha256Digest,
            ["DeliverySecurity:KeyDigests:site-B"] = other.Sha256Digest
        } };
        using var first = factory.CreateAnonymousClient();
        foreach (var key in new[] { old.Key, other.Key })
        {
            DeliveryAuthorizationTests.SetKey(first, key);
            using var response = await first.GetAsync("/api/v1/content/root/children", ct);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        await using var restarted = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        { ["DeliverySecurity:KeyDigests:site-A"] = next.Sha256Digest })));
        using var client = restarted.CreateClient(new() { BaseAddress = new("https://localhost") });
        foreach (var (key, status) in new[] { (old.Key, HttpStatusCode.Unauthorized), (next.Key, HttpStatusCode.OK), (other.Key, HttpStatusCode.OK) })
        {
            DeliveryAuthorizationTests.SetKey(client, key);
            using var response = await client.GetAsync("/api/v1/content/root/children", ct);
            Assert.Equal(status, response.StatusCode);
        }
        await using var empty = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        { ["DeliverySecurity:KeyDigests:site-A"] = null, ["DeliverySecurity:KeyDigests:site-B"] = null })));
        using var none = empty.CreateClient(new() { BaseAddress = new("https://localhost") });
        DeliveryAuthorizationTests.SetKey(none, next.Key);
        using var denied = await none.GetAsync("/api/v1/content/root/children", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
    }
}
