using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using TemplarCMS.Api.Security;

namespace TemplarCMS.Api.Tests.Support;

internal sealed class DeliveryApiFactory : AdminIdentityApiFactory
{
    public GeneratedDeliveryKey SiteKey { get; } = DeliveryKeyGenerator.Generate();
    public Dictionary<string, string?>? Overrides { get; init; }
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(Overrides ?? new()
        {
            ["DeliverySecurity:KeyDigests:site-A"] = SiteKey.Sha256Digest,
            ["OpenApi:Enabled"] = "true"
        }));
    }
}
