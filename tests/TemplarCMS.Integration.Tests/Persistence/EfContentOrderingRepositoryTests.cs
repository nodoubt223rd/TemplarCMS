using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Domain.Content;
using TemplarCMS.Persistence;
using TemplarCMS.Persistence.Content;
using Xunit;

namespace TemplarCMS.Integration.Tests.Persistence;

public sealed class EfContentOrderingRepositoryTests
{
    [Fact]
    public async Task ConcurrentWritersUsingSameRevisionHaveOneWinner()
    {
        var ct = TestContext.Current.CancellationToken;
        var path = Path.Combine(Path.GetTempPath(), "templar-order-" + Guid.NewGuid().ToString("N") + ".db");
        var options = new DbContextOptionsBuilder<TemplarCmsDbContext>().UseSqlite($"Data Source={path};Pooling=False").Options;
        var templateId = new TemplateId(Guid.NewGuid());
        var field = new FieldDefinition(new FieldId(Guid.NewGuid()), "Sort", "__sortorder", FieldType.SingleLineText, isShared: true);
        var catalog = new TestCatalog(new EffectiveTemplateDefinition(templateId, "Page", new TemplateKey("page"),
            [new TemplateSectionDefinition(Guid.NewGuid(), "Appearance", "appearance", 0, [field])]));
        var id = Guid.NewGuid();
        try
        {
            string revision;
            await using (var setup = new TemplarCmsDbContext(options))
            {
                await setup.Database.EnsureCreatedAsync(ct);
                setup.ContentItems.AddRange(new PersistenceContentItem { Id = Guid.NewGuid(), Name = "A", Key = "a", TemplateId = templateId.Value },
                    new PersistenceContentItem { Id = id, Name = "B", Key = "b", TemplateId = templateId.Value });
                await setup.SaveChangesAsync(ct);
                revision = (await new EfContentOrderingRepository(setup, catalog).ReadAsync(null, ct)).Revision;
            }
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            async Task<bool> WriteAsync()
            {
                await start.Task;
                await using var context = new TemplarCmsDbContext(options);
                try { await new EfContentOrderingRepository(context, catalog).ReorderAsync(id, ContentOrderDirection.First, revision, ct); return true; }
                catch (ContentOrderConflictException) { return false; }
            }
            var first = Task.Run(WriteAsync, ct);
            var second = Task.Run(WriteAsync, ct);
            start.SetResult();
            Assert.Single(await Task.WhenAll(first, second), success => success);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public async Task ReorderPersistsSharedValuesAndRejectsStaleRevision()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        await using var db = new TemplarCmsDbContext(new DbContextOptionsBuilder<TemplarCmsDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync(ct);
        var templateId = new TemplateId(Guid.NewGuid());
        var fieldId = new FieldId(Guid.NewGuid());
        var field = new FieldDefinition(fieldId, "Sort order", "__sortorder", FieldType.SingleLineText, isShared: true);
        var catalog = new TestCatalog(new EffectiveTemplateDefinition(
            templateId, "Page", new TemplateKey("page"), [new TemplateSectionDefinition(Guid.NewGuid(), "Appearance", "appearance", 0, [field])]));
        var items = new[] { "a", "b", "c" }.Select(key => new PersistenceContentItem
            { Id = Guid.NewGuid(), TemplateId = templateId.Value, Name = key, Key = key }).ToArray();
        db.ContentItems.AddRange(items);
        await db.SaveChangesAsync(ct);
        var repository = new EfContentOrderingRepository(db, catalog);
        var before = await repository.ReadAsync(null, ct);
        var after = await repository.ReorderAsync(items[2].Id, ContentOrderDirection.First, before.Revision, ct);
        Assert.Equal(new[] { "c", "a", "b" }, after.Items.Select(i => i.Key));
        Assert.All(await db.ContentFieldValues.ToListAsync(ct), value =>
        {
            Assert.Equal("shared", value.Language);
            Assert.Equal(0, value.Version);
        });
        await Assert.ThrowsAsync<ContentOrderConflictException>(() => repository.ReorderAsync(items[0].Id, ContentOrderDirection.Last, before.Revision, ct));
        Assert.Equal(after.Revision, (await repository.ReadAsync(null, ct)).Revision);
        var noop = await repository.ReorderAsync(items[2].Id, ContentOrderDirection.Up, after.Revision, ct);
        Assert.Equal(after.Revision, noop.Revision);
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER fail_order_write BEFORE UPDATE ON ContentFieldValues
            BEGIN SELECT RAISE(ABORT, 'Injected order write failure'); END;
            """, ct);
        await Assert.ThrowsAsync<DbUpdateException>(() => repository.ReorderAsync(items[2].Id, ContentOrderDirection.Last, after.Revision, ct));
        Assert.Equal(after.Revision, (await repository.ReadAsync(null, ct)).Revision);
    }

    private sealed class TestCatalog(EffectiveTemplateDefinition template) : IContentModelCatalog
    {
        public Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(TemplateId id, CancellationToken cancellationToken = default) => Task.FromResult<EffectiveTemplateDefinition?>(template);
        public Task<EffectiveTemplateDefinition?> GetEffectiveTemplateAsync(TemplateKey key, CancellationToken cancellationToken = default) => Task.FromResult<EffectiveTemplateDefinition?>(template);
        public Task<IReadOnlyCollection<EffectiveTemplateDefinition>> GetEffectiveTemplatesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<EffectiveTemplateDefinition>>([template]);
        public Task<TemplateDefinition?> GetTemplateAsync(TemplateId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<TemplateDefinition?> GetTemplateAsync(TemplateKey key, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task InvalidateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
