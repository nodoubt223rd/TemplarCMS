using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TemplarCMS.Abstractions.Security;

namespace TemplarCMS.Api.Tests.Support;

internal class AdminIdentityApiFactory : WebApplicationFactory<Program>
{
    protected readonly string Root = Directory.CreateTempSubdirectory("TemplarCMS.IdentityHttpTests-").FullName;
    public string? TemporaryPassword { get; private set; }
    public Guid UserId { get; private set; }
    private string? _currentPassword;
    internal const string ChangedTestPassword = "My new secure passphrase ✓";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Persistence:Provider"] = "Sqlite", ["ConnectionStrings:TemplarCms"] = $"Data Source={Path.Combine(Root, "cms.db")};Pooling=False",
            ["Templates:TemplatesPath"] = Path.Combine(Root, "Templates"), ["AuthoringSecurity:Enabled"] = "true",
            ["IdentitySecurity:DataProtectionKeysPath"] = Path.Combine(Root, "Keys"), ["UserDirectory:Enabled"] = "true",
            ["OpenApi:Enabled"] = "false"
        }));
    }
    public HttpClient CreateAnonymousClient() => CreateClient(new() { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    public async Task ProvisionAsync(CancellationToken ct)
    {
        using var scope = Services.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<IAdminProvisioningService>().SetupAsync("admin@example.test", ct);
        if (result.UserId is { } id) UserId = id;
        if (result.TemporaryPassword is not null) { TemporaryPassword = result.TemporaryPassword; _currentPassword = result.TemporaryPassword; }
        else if (result.Status == TemplarCMS.Domain.Security.AdminProvisioningStatus.AlreadyInitialized) _currentPassword ??= ChangedTestPassword;
    }
    public async Task<HttpClient> CreateAdminClientAsync(bool changePassword, CancellationToken ct)
    {
        await ProvisionAsync(ct);
        var client = CreateAnonymousClient();
        await RefreshCsrfAsync(client, ct);
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { loginName = "TemplarAdmin", password = _currentPassword }, ct);
        login.EnsureSuccessStatusCode();
        await RefreshCsrfAsync(client, ct);
        if (changePassword && (await login.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("mustChangePassword").GetBoolean())
        {
            using var change = await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = _currentPassword, newPassword = ChangedTestPassword }, ct);
            change.EnsureSuccessStatusCode();
            _currentPassword = ChangedTestPassword;
            await RefreshCsrfAsync(client, ct);
        }
        return client;
    }
    public static async Task RefreshCsrfAsync(HttpClient client, CancellationToken ct)
    {
        var response = await client.GetFromJsonAsync<JsonElement>("/api/v1/auth/antiforgery", ct);
        client.DefaultRequestHeaders.Remove("X-Templar-CSRF");
        client.DefaultRequestHeaders.Add("X-Templar-CSRF", response.GetProperty("requestToken").GetString());
    }
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (!Root.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe test cleanup path.");
        if (Directory.Exists(Root)) Directory.Delete(Root, true);
    }
}
