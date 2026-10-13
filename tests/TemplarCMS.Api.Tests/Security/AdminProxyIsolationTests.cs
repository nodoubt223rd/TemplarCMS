extern alias AdminServer;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace TemplarCMS.Api.Tests.Security;

public sealed class AdminProxyIsolationTests
{
    [Fact]
    public async Task PooledProxyNeverSharesBrowserCookiesAndPreservesAntiforgery()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var backend = await ProbeBackend.StartAsync(ct);
        await using var factory = new ProxyFactory(backend.Address);
        using var first = factory.Browser();
        using var second = factory.Browser();
        using var anonymous = factory.Browser();
        Assert.Equal(backend.Address, factory.Services.GetRequiredService<IHttpClientFactory>().CreateClient("TemplarApiProxy").BaseAddress!.ToString().TrimEnd('/'));
        using var firstLogin = await first.PostAsync("/api/login?name=first", null, ct);
        Assert.True(firstLogin.IsSuccessStatusCode, await firstLogin.Content.ReadAsStringAsync(ct));
        Assert.Equal(2, firstLogin.Headers.GetValues("Set-Cookie").Count());
        using var secondLogin = await second.PostAsync("/api/login?name=second", null, ct);
        Assert.Equal(HttpStatusCode.OK, secondLogin.StatusCode);
        first.DefaultRequestHeaders.Add("X-Templar-CSRF", "first-request-token");
        var firstSession = await first.GetFromJsonAsync<JsonElement>("/api/session", ct);
        var secondSession = await second.GetFromJsonAsync<JsonElement>("/api/session", ct);
        var anonymousSession = await anonymous.GetFromJsonAsync<JsonElement>("/api/session", ct);
        Assert.Equal("first", firstSession.GetProperty("name").GetString());
        Assert.Equal("second", secondSession.GetProperty("name").GetString());
        Assert.Equal("anonymous", anonymousSession.GetProperty("name").GetString());
        Assert.Equal("first-request-token", firstSession.GetProperty("csrf").GetString());
        Assert.All(firstLogin.Headers.GetValues("Set-Cookie"), cookie =>
        {
            Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task BackendRedirectIsReturnedWithoutFollowingIt()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var backend = await ProbeBackend.StartAsync(ct);
        await using var factory = new ProxyFactory(backend.Address);
        using var client = factory.Browser();
        Assert.Equal(backend.Address, factory.Services.GetRequiredService<IHttpClientFactory>().CreateClient("TemplarApiProxy").BaseAddress!.ToString().TrimEnd('/'));
        using var response = await client.GetAsync("/api/redirect", ct);
        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/api/session", response.Headers.Location!.ToString());
    }

    private sealed class ProxyFactory(string target) : WebApplicationFactory<AdminServer::Program>
    {
        public HttpClient Browser() => CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["TemplarApi:BaseUrl"] = target }));
        }
    }

    private sealed class ProbeBackend(WebApplication app, X509Certificate2 certificate) : IAsyncDisposable
    {
        public string Address => app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        public static async Task<ProbeBackend> StartAsync(CancellationToken ct)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var names = new SubjectAlternativeNameBuilder();
            names.AddDnsName("localhost"); names.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(names.Build());
            using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
            // SChannel requires a usable key provider for the loopback server certificate.
            var certificate = new X509Certificate2(generated.Export(X509ContentType.Pfx), "", X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable);
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0, listen => listen.UseHttps(certificate)));
            var app = builder.Build();
            app.MapPost("/api/login", (HttpContext http) =>
            {
                var options = new CookieOptions { Path = "/", Secure = true, HttpOnly = true, SameSite = SameSiteMode.Strict };
                http.Response.Cookies.Append("__Host-proxy-auth", http.Request.Query["name"].ToString(), options);
                http.Response.Cookies.Append("__Host-proxy-csrf", "cookie-token", options);
                return Results.Ok();
            });
            app.MapGet("/api/session", (HttpContext http) => Results.Json(new { name = http.Request.Cookies["__Host-proxy-auth"] ?? "anonymous", csrf = http.Request.Headers["X-Templar-CSRF"].ToString() }));
            app.MapGet("/api/redirect", () => Results.Redirect("/api/session"));
            await app.StartAsync(ct);
            return new(app, certificate);
        }
        public async ValueTask DisposeAsync() { await app.StopAsync(); await app.DisposeAsync(); certificate.Dispose(); }
    }
}
