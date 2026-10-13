using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TemplarCMS.Abstractions.Security;
using TemplarCMS.Api.Tests.Support;
using TemplarCMS.Persistence;
using TemplarCMS.Persistence.Security;
using Xunit;

namespace TemplarCMS.Api.Tests.Security;

public sealed class AdminAuthenticationBoundaryTests
{
    [Fact]
    public async Task FiveFailuresLockForFifteenMinutesWithoutEnumeratingAccounts()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AdminIdentityApiFactory();
        await factory.ProvisionAsync(ct);
        using var client = factory.CreateAnonymousClient();
        await AdminIdentityApiFactory.RefreshCsrfAsync(client, ct);
        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var wrong = await client.PostAsJsonAsync("/api/v1/auth/login", new { loginName = "TemplarAdmin", password = "wrong password" }, ct);
            Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        }
        using var correct = await client.PostAsJsonAsync("/api/v1/auth/login", new { loginName = "TemplarAdmin", password = factory.TemporaryPassword }, ct);
        Assert.Equal(HttpStatusCode.Unauthorized, correct.StatusCode);
        using var unknown = await client.PostAsJsonAsync("/api/v1/auth/login", new { loginName = "unknown", password = "wrong password" }, ct);
        var lockedProblem = await correct.Content.ReadFromJsonAsync<JsonElement>(ct);
        var unknownProblem = await unknown.Content.ReadFromJsonAsync<JsonElement>(ct);
        Assert.Equal(lockedProblem.GetProperty("detail").GetString(), unknownProblem.GetProperty("detail").GetString());
        using var scope = factory.Services.CreateScope();
        var account = await scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>().Users.AsNoTracking().SingleAsync(ct);
        Assert.InRange(account.LockoutEnd!.Value - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(16));
    }

    [Fact]
    public async Task LoginRateLimitRejectsEleventhRequest()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new AdminIdentityApiFactory();
        using var client = factory.CreateAnonymousClient();
        await AdminIdentityApiFactory.RefreshCsrfAsync(client, ct);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { loginName = "unknown", password = "wrong password" }, ct);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        using var limited = await client.PostAsJsonAsync("/api/v1/auth/login", new { loginName = "unknown", password = "wrong password" }, ct);
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.CacheControl!.NoStore);
    }

    [Fact]
    public async Task SessionExpiresAtThirtyMinutesWithoutSlidingRenewal()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new ClockFactory();
        using var client = await factory.CreateAdminClientAsync(false, ct);
        factory.Clock.Advance(TimeSpan.FromMinutes(20));
        using var stillValid = await client.GetAsync("/api/v1/auth/session", ct);
        Assert.Equal(HttpStatusCode.OK, stillValid.StatusCode);
        factory.Clock.Advance(TimeSpan.FromMinutes(11));
        using var expired = await client.GetAsync("/api/v1/auth/session", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
    }

    [Fact]
    public async Task ResetRacingWithPasswordVerificationCannotMintANewerValidSession()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var factory = new RaceFactory();
        await factory.ProvisionAsync(ct);
        using var client = factory.CreateAnonymousClient();
        await AdminIdentityApiFactory.RefreshCsrfAsync(client, ct);
        using var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { loginName = "TemplarAdmin", password = factory.TemporaryPassword }, ct);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        using var session = await client.GetAsync("/api/v1/auth/session", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, session.StatusCode);
    }

    private sealed class ClockFactory : AdminIdentityApiFactory
    {
        public ManualClock Clock { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(Clock));
        }
    }
    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }
    private sealed class RaceFactory : AdminIdentityApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services => services.AddScoped<UserManager<CmsIdentityUser>, ResetRacingUserManager>());
        }
    }
    private sealed class ResetRacingUserManager : UserManager<CmsIdentityUser>
    {
        private readonly IServiceScopeFactory _scopes;
        public ResetRacingUserManager(IUserStore<CmsIdentityUser> store, IOptions<IdentityOptions> options, IPasswordHasher<CmsIdentityUser> hasher,
            IEnumerable<IUserValidator<CmsIdentityUser>> userValidators, IEnumerable<IPasswordValidator<CmsIdentityUser>> passwordValidators,
            ILookupNormalizer normalizer, IdentityErrorDescriber errors, IServiceProvider services, ILogger<UserManager<CmsIdentityUser>> logger,
            IServiceScopeFactory scopes) : base(store, options, hasher, userValidators, passwordValidators, normalizer, errors, services, logger) => _scopes = scopes;
        public override async Task<bool> CheckPasswordAsync(CmsIdentityUser user, string password)
        {
            var matches = await base.CheckPasswordAsync(user, password);
            if (matches)
            {
                using var scope = _scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IAdminProvisioningService>().ResetAsync(CancellationToken.None);
            }
            return matches;
        }
    }
}
