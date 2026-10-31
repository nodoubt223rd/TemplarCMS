using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TemplarCMS.Abstractions.Security;
using TemplarCMS.Api.Bootstrap;
using TemplarCMS.Api.Security;
using TemplarCMS.Domain.Security;

namespace TemplarCMS.Api.Commands;

public static class AdminCommandRunner
{
    public static bool IsCommand(string[] args) => args.Length > 0 && !args[0].StartsWith('-');

    public static async Task<int> RunAsync(IServiceProvider services, string[] args, TextWriter output, TextWriter error, CancellationToken ct)
    {
        if (args is ["generate-delivery-key"])
        {
            try
            {
                var generated = DeliveryKeyGenerator.Generate();
                output.WriteLine($"Delivery key: {generated.Key}\nSHA-256 digest: {generated.Sha256Digest}\nStore the key on the consuming server and the named digest in CMS configuration. This key will not be displayed again.");
                return 0;
            }
            catch (IOException) { error.WriteLine("Delivery key output failed. Run generate-delivery-key again; no credential was configured."); return 1; }
        }
        var setup = args.Length == 3 && args[0] == "setup-admin" && args[1] == "--email";
        var reset = args.Length == 1 && args[0] == "reset-admin-password";
        if (!setup && !reset)
        {
            error.WriteLine("Use setup-admin --email <operator-email>, reset-admin-password, or generate-delivery-key. The HTTP server was not started.");
            return 2;
        }
        var committed = false;
        try
        {
            await services.GetRequiredService<CmsRuntimeInitializer>().InitializeAsync(ct);
            await using var scope = services.CreateAsyncScope();
            var provisioner = scope.ServiceProvider.GetRequiredService<IAdminProvisioningService>();
            var result = setup ? await provisioner.SetupAsync(args[2], ct) : await provisioner.ResetAsync(ct);
            if (result.Status == AdminProvisioningStatus.AlreadyInitialized)
            {
                output.WriteLine("Administrator setup has already completed. Existing credentials were left unchanged.");
                return 0;
            }
            if (result.Status != AdminProvisioningStatus.Created)
            {
                error.WriteLine($"Administrator command could not complete: {result.Status}. No password was issued.");
                return 1;
            }
            committed = true;
            output.WriteLine($"Login: {AdminProvisioningService.LoginName}\nTemporary password: {result.TemporaryPassword}\nChange this password at first login. This password will not be displayed again.");
            return 0;
        }
        catch (IOException)
        {
            error.WriteLine(committed ? "Administrator credentials were committed, but terminal output failed. Use reset-admin-password to issue a replacement." : "Administrator setup failed before password delivery. Check runtime storage access.");
            return 1;
        }
        catch (Exception exception) when (exception is InvalidOperationException or DbUpdateException or SqlException or SqliteException)
        {
            // Never echo SQL, exception details, or command arguments to secret-bearing output.
            error.WriteLine("Administrator command failed. Check database access and apply the required schema scripts, including 007-admin-identity.sql for SQL Server.");
            return 1;
        }
    }
}
