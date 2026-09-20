using TemplarCMS.ContentModeling.Organization;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Organization;

public sealed class TemplateOrganizationTests
{
    [Fact]
    public void Validator_RejectsMissingParentsAndSelfPlacement()
    {
        var id = new TemplateFolderId(Guid.NewGuid());
        var missing = new TemplateFolderId(Guid.NewGuid());
        Assert.False(TemplateOrganizationValidator.Validate(new(Guid.Empty,
            [new(id, "Pages", "pages", missing)], [])).IsValid);
        Assert.False(TemplateOrganizationValidator.Validate(new(Guid.Empty,
            [new(id, "Pages", "pages", id)], [])).IsValid);
        Assert.False(TemplateOrganizationValidator.Validate(new(Guid.Empty,
            [], [new(new TemplateId(Guid.NewGuid()), missing)])).IsValid);
        Assert.Throws<ArgumentException>(() => new TemplateFolderId(Guid.Empty));
    }

    [Fact]
    public async Task Repository_ConcurrentWritersHaveExactlyOneWinner()
    {
        var path = Path.Combine(Path.GetTempPath(), "templar-organization-" + Guid.NewGuid().ToString("N"));
        try
        {
            async Task<bool> WriteAsync()
            {
                try
                {
                    await new JsonTemplateOrganizationRepository(path).SaveAsync(
                        TemplateOrganizationSnapshot.Empty, Guid.Empty, TestContext.Current.CancellationToken);
                    return true;
                }
                catch (TemplateOrganizationConflictException) { return false; }
            }
            var results = await Task.WhenAll(WriteAsync(), WriteAsync());
            Assert.Single(results, success => success);
            Assert.NotEqual(Guid.Empty, (await new JsonTemplateOrganizationRepository(path).ReadAsync(TestContext.Current.CancellationToken)).Revision);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }

    [Fact]
    public async Task Repository_FailedReplacementPreservesPreviousDocument()
    {
        var path = Path.Combine(Path.GetTempPath(), "templar-organization-" + Guid.NewGuid().ToString("N"));
        try
        {
            var repository = new JsonTemplateOrganizationRepository(path);
            await repository.SaveAsync(TemplateOrganizationSnapshot.Empty, Guid.Empty, TestContext.Current.CancellationToken);
            var original = await repository.ReadAsync(TestContext.Current.CancellationToken);
            var dataPath = Path.Combine(path, "Organization", "tree.json");
            // Deny replacement while allowing readers, reproducing a Windows file-lock failure.
            if (OperatingSystem.IsWindows())
            {
                using (var held = new FileStream(dataPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    await Assert.ThrowsAsync<UnauthorizedAccessException>(() => repository.SaveAsync(original, original.Revision, TestContext.Current.CancellationToken));
                Assert.Equal(original.Revision, (await repository.ReadAsync(TestContext.Current.CancellationToken)).Revision);
                Assert.Empty(Directory.GetFiles(repository.DirectoryPath, "*.tmp"));
            }
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.SaveAsync(original, original.Revision, cancelled.Token));
            Assert.Equal(original.Revision, (await repository.ReadAsync(TestContext.Current.CancellationToken)).Revision);
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }

    [Fact]
    public void Validator_RejectsCyclesAndDuplicateSiblingKeys()
    {
        var first = new TemplateFolderId(Guid.NewGuid());
        var second = new TemplateFolderId(Guid.NewGuid());
        var cycle = new TemplateOrganizationSnapshot(Guid.NewGuid(),
            [new(first, "First", "first", second), new(second, "Second", "second", first)], []);
        Assert.False(TemplateOrganizationValidator.Validate(cycle).IsValid);
        var duplicate = new TemplateOrganizationSnapshot(Guid.NewGuid(),
            [new(first, "One", "same", null), new(second, "Two", "SAME", null)], []);
        Assert.False(TemplateOrganizationValidator.Validate(duplicate).IsValid);
    }

    [Fact]
    public async Task Repository_RoundTripsAndRejectsStaleWritesAcrossInstances()
    {
        var path = Path.Combine(Path.GetTempPath(), "templar-organization-" + Guid.NewGuid().ToString("N"));
        try
        {
            var first = new JsonTemplateOrganizationRepository(path);
            var second = new JsonTemplateOrganizationRepository(path);
            var empty = await first.ReadAsync(TestContext.Current.CancellationToken);
            Assert.Empty(empty.Folders);
            var id = new TemplateFolderId(Guid.NewGuid());
            var child = new TemplateFolderId(Guid.NewGuid());
            var template = new TemplateId(Guid.NewGuid());
            var changed = new TemplateOrganizationSnapshot(Guid.NewGuid(),
                [new(id, "Pages", "pages", null), new(child, "Landing", "landing", id)], [new(template, child)]);
            await first.SaveAsync(changed, empty.Revision, TestContext.Current.CancellationToken);
            var saved = await second.ReadAsync(TestContext.Current.CancellationToken);
            Assert.Equal(id, saved.Folders[1].ParentId);
            Assert.Equal(template, Assert.Single(saved.Placements).TemplateId);
            var definitions = new TemplarCMS.ContentModeling.Repositories.JsonTemplateRepository(
                Microsoft.Extensions.Options.Options.Create(new TemplarCMS.ContentModeling.Repositories.JsonTemplateRepositoryOptions { TemplatesPath = path }),
                new TemplarCMS.ContentModeling.Serialization.JsonTemplateMapper());
            Assert.Empty(await definitions.GetTemplatesAsync(TestContext.Current.CancellationToken));
            await Assert.ThrowsAsync<TemplateOrganizationConflictException>(() => second.SaveAsync(
                changed, empty.Revision, TestContext.Current.CancellationToken));
        }
        finally { if (Directory.Exists(path)) Directory.Delete(path, true); }
    }

    [Fact]
    public async Task Repository_DoesNotSilentlyResetMalformedData()
    {
        var path = Path.Combine(Path.GetTempPath(), "templar-organization-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(path, "Organization"));
        try
        {
            await File.WriteAllTextAsync(Path.Combine(path, "Organization", "tree.json"), "broken", TestContext.Current.CancellationToken);
            await Assert.ThrowsAnyAsync<Exception>(() => new JsonTemplateOrganizationRepository(path).ReadAsync(TestContext.Current.CancellationToken));
        }
        finally { Directory.Delete(path, true); }
    }
}

