using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TemplarCMS.Persistence;
using TemplarCMS.Api.Tests.Security;
using Xunit;

namespace TemplarCMS.Api.Tests.Commands;

public sealed class AdminCommandTests
{
    [Fact]
    public async Task SetupDisplaysPasswordOnceAndRepeatedSetupDoesNotResetIt()
    {
        await using var fixture = new ProvisioningFixture();
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.Equal(0, await Run(fixture, ["setup-admin", "--email", "admin@example.test"], output, error));
        Assert.Contains("Temporary password:", output.ToString());
        output.GetStringBuilder().Clear();
        Assert.Equal(0, await Run(fixture, ["setup-admin", "--email", "admin@example.test"], output, error));
        Assert.DoesNotContain("Temporary password:", output.ToString());
        Assert.Empty(error.ToString());
    }

    [Fact]
    public async Task InvalidArgumentsAndMissingSchemaNeverDisplaySecrets()
    {
        await using var fixture = new ProvisioningFixture();
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.NotEqual(0, await Run(fixture, ["setup-admin"], output, error));
        Assert.Empty(output.ToString());
        Assert.NotEqual(0, await Run(fixture, ["unknown-command"], output, error));
        Assert.DoesNotContain("Temporary password:", output.ToString());
    }

    [Fact]
    public async Task LostOutputLeavesAccountInitializedAndRequiresExplicitRecovery()
    {
        await using var fixture = new ProvisioningFixture();
        using var error = new StringWriter();
        Assert.NotEqual(0, await Run(fixture, ["setup-admin", "--email", "admin@example.test"], new FailingWriter(), error));
        using var output = new StringWriter();
        Assert.Equal(0, await Run(fixture, ["setup-admin", "--email", "admin@example.test"], output, error));
        Assert.DoesNotContain("Temporary password:", output.ToString());
        output.GetStringBuilder().Clear();
        Assert.Equal(0, await Run(fixture, ["reset-admin-password"], output, error));
        Assert.Contains("Temporary password:", output.ToString());
    }

    [Fact]
    public async Task ProvisioningFailureRollsBackProfileCredentialAndMarkerWithoutOutput()
    {
        await using var fixture = new ProvisioningFixture();
        using (var scope = fixture.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<TemplarCmsDbContext>().Database.ExecuteSqlRawAsync("CREATE TRIGGER reject_admin_marker BEFORE INSERT ON AdminProvisioning BEGIN SELECT RAISE(ABORT, 'Simulated write failure'); END;", TestContext.Current.CancellationToken);
        using var output = new StringWriter();
        using var error = new StringWriter();
        Assert.NotEqual(0, await Run(fixture, ["setup-admin", "--email", "admin@example.test"], output, error));
        Assert.Empty(output.ToString());
        using var check = fixture.Services.CreateScope();
        var db = check.ServiceProvider.GetRequiredService<TemplarCmsDbContext>();
        Assert.Empty(await db.DirectoryUsers.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await db.Users.ToListAsync(TestContext.Current.CancellationToken));
        Assert.Empty(await db.AdminProvisioning.ToListAsync(TestContext.Current.CancellationToken));
    }

    private static Task<int> Run(ProvisioningFixture fixture, string[] args, TextWriter output, TextWriter error)
    {
        var type = typeof(TemplarCMS.Api.Security.AuthoringSecurityServiceCollectionExtensions).Assembly.GetType("TemplarCMS.Api.Commands.AdminCommandRunner");
        Assert.NotNull(type);
        var method = type.GetMethod("RunAsync", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        return (Task<int>)method.Invoke(null, [fixture.Services, args, output, error, TestContext.Current.CancellationToken])!;
    }

    private sealed class FailingWriter : StringWriter
    {
        public override void WriteLine(string? value) => throw new IOException("Simulated terminal output failure.");
    }
}
