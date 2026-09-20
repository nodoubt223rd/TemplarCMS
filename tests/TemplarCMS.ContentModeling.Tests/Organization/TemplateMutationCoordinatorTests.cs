using TemplarCMS.ContentModeling.Organization;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Organization;

public sealed class TemplateMutationCoordinatorTests
{
    [Fact]
    public async Task FailedMutationRestoresDefinitionAndHierarchy()
    {
        var path = Path.Combine(Path.GetTempPath(), "templar-journal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        var token = TestContext.Current.CancellationToken;
        try
        {
            var definition = Path.Combine(path, "page.json");
            await File.WriteAllTextAsync(definition, "original", token);
            var repository = new JsonTemplateOrganizationRepository(path);
            var coordinator = new TemplateMutationCoordinator(path, repository);
            await Assert.ThrowsAsync<InvalidOperationException>(() => coordinator.ExecuteAsync(async ct =>
            {
                File.Delete(definition);
                await File.WriteAllTextAsync(Path.Combine(path, "new.json"), "new", ct);
                await repository.SaveLockedAsync(TemplateOrganizationSnapshot.Empty, Guid.Empty, ct);
                throw new InvalidOperationException("Injected failure after writes");
            }, token));
            Assert.Equal("original", await File.ReadAllTextAsync(definition, token));
            Assert.False(File.Exists(Path.Combine(path, "new.json")));
            Assert.Equal(Guid.Empty, (await repository.ReadAsync(token)).Revision);
        }
        finally { Directory.Delete(path, true); }
    }

    [Fact]
    public async Task NewCoordinatorRecoversInterruptedMutation()
    {
        var path = Path.Combine(Path.GetTempPath(), "templar-journal-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        var token = TestContext.Current.CancellationToken;
        try
        {
            var definition = Path.Combine(path, "page.json");
            await File.WriteAllTextAsync(definition, "original", token);
            var repository = new JsonTemplateOrganizationRepository(path);
            var coordinator = new TemplateMutationCoordinator(path, repository);
            await using (var gate = await repository.AcquireAsync(token))
            {
                await coordinator.BeginLockedAsync(token);
                File.Delete(definition);
                await File.WriteAllTextAsync(Path.Combine(path, "orphan.json"), "orphan", token);
            }
            await new TemplateMutationCoordinator(path, repository).RecoverAsync(token);
            Assert.Equal("original", await File.ReadAllTextAsync(definition, token));
            Assert.False(File.Exists(Path.Combine(path, "orphan.json")));
            Assert.False(File.Exists(Path.Combine(repository.DirectoryPath, "pending.json")));
        }
        finally { Directory.Delete(path, true); }
    }
}
