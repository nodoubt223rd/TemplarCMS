using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TemplarCMS.Domain.Content;
using TemplarCMS.Persistence;
using TemplarCMS.Persistence.Content;
using TemplarCMS.Persistence.Media;
using Xunit;

namespace TemplarCMS.Integration.Tests.Persistence;

public sealed class SqlPolicyTests
{
    [Theory]
    [InlineData(255)]
    [InlineData(450)]
    [InlineData(500)]
    public void LengthChecksCountUtf16AndRejectWithoutTruncation(int limit)
    {
        var exact = "\U0001F600" + new string('a', limit - 2);
        AuthoringLimits.Check(exact, limit, "value");
        Assert.Throws<ArgumentException>(() => AuthoringLimits.Check(exact + "a", limit, "value"));
    }

    [Fact]
    public async Task PersistenceValidatesBoundsAndMaintainsFieldSaveTimestamps()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var db = new TemplarCmsDbContext(new DbContextOptionsBuilder<TemplarCmsDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        var repository = new EfContentRepository(db);
        var id = new ContentItemId(Guid.NewGuid());
        await repository.SaveItemAsync(new ContentItemDefinition(id, new string('n',255), new ContentItemKey("page"), new TemplateId(Guid.NewGuid())), TestContext.Current.CancellationToken);
        var item = await db.ContentItems.SingleAsync(TestContext.Current.CancellationToken);
        var created = item.CreatedUtc;
        var modified = item.ModifiedUtc;
        Assert.Equal(TimeSpan.Zero, created.Offset);
        await repository.SaveFieldValuesAsync(id, [new ContentFieldValue(id,new FieldId(Guid.NewGuid()),"body",new ContentLanguage("en"),new ContentVersion(1),"text")], TestContext.Current.CancellationToken);
        Assert.Equal(created,item.CreatedUtc);
        Assert.True(item.ModifiedUtc > modified);
        item.Name = new string('n',256);
        await Assert.ThrowsAsync<ArgumentException>(() => db.SaveChangesAsync(TestContext.Current.CancellationToken));
        item.Name = "valid";
        item.CreatedUtc = created.AddDays(-1);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await db.Entry(item).ReloadAsync(TestContext.Current.CancellationToken);
        Assert.Equal(created,item.CreatedUtc);
    }

    [Fact]
    public void SchemaVerifierRejectsDriftAndAcceptsSqlFilterFormatting()
    {
        using var db = new TemplarCmsDbContext(new DbContextOptionsBuilder<TemplarCmsDbContext>().UseSqlServer("Server=unused;Database=model").Options);
        var columns = new List<SqlServerSchemaVerifier.ColumnShape>();
        var indexes = new List<SqlServerSchemaVerifier.IndexShape>();
        foreach (var entity in db.Model.GetEntityTypes())
        {
            var table = entity.GetTableName()!;
            var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
            foreach (var p in entity.GetProperties())
            {
                var type = p.GetColumnType()!;
                columns.Add(new() { TableName=table, ColumnName=p.GetColumnName(store)!, TypeName=type.Split('(')[0], MaxLength=type.StartsWith("nvarchar") ? p.GetMaxLength() is {} max ? max*2 : -1 : 0, IsNullable=p.IsNullable, Scale=7, CollationName=SqlServerSchemaVerifier.Collation });
            }
            indexes.Add(new() { TableName=table, IndexName="PK_"+table, IsUnique=true, IsPrimaryKey=true, KeyColumns=string.Join(',',entity.FindPrimaryKey()!.Properties.Select(p=>p.GetColumnName(store))) });
            indexes.AddRange(entity.GetIndexes().Select(i=>new SqlServerSchemaVerifier.IndexShape {TableName=table,IndexName=i.GetDatabaseName()!,IsUnique=i.IsUnique,KeyColumns=string.Join(',',i.Properties.Select(p=>p.GetColumnName(store))),FilterDefinition=i.GetFilter() is {} filter ? "("+filter+")" : null}));
        }
        SqlServerSchemaVerifier.Validate(db.Model,columns,indexes);
        var name = columns.Single(c=>c.TableName=="ContentItems" && c.ColumnName=="Name");
        Assert.Equal(510,name.MaxLength);
        name.MaxLength = -1;
        Assert.Throws<InvalidOperationException>(()=>SqlServerSchemaVerifier.Validate(db.Model,columns,indexes));
        name.MaxLength = 510;
        var root = indexes.Single(i=>i.IndexName=="IX_ContentItems_Root_Key");
        root.IsDisabled = true;
        Assert.Throws<InvalidOperationException>(()=>SqlServerSchemaVerifier.Validate(db.Model,columns,indexes));
        root.IsDisabled = false;
        name.CollationName = "SQL_Latin1_General_CP1_CI_AI";
        Assert.Throws<InvalidOperationException>(()=>SqlServerSchemaVerifier.Validate(db.Model,columns,indexes));
    }

    [Fact]
    public async Task RehearsalDatabasePassesReadOnlySchemaAndComparisonChecks()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEMPLARCMS_SQL_REHEARSAL_CONNECTION");
        if (string.IsNullOrEmpty(connectionString)) Assert.Skip("Set TEMPLARCMS_SQL_REHEARSAL_CONNECTION for the opt-in SQL test.");
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
        Assert.StartsWith("templarcms_rehearsal_",builder.InitialCatalog);
        await using var db = new TemplarCmsDbContext(new DbContextOptionsBuilder<TemplarCmsDbContext>().UseSqlServer(connectionString).Options);
        await SqlServerSchemaVerifier.VerifyAsync(db,TestContext.Current.CancellationToken);
        var comparisons = await db.Database.SqlQueryRaw<int>("SELECT CASE WHEN N'Home' = N'home' COLLATE SQL_Latin1_General_CP1_CI_AS AND N'resume' <> N'résumé' COLLATE SQL_Latin1_General_CP1_CI_AS THEN 1 ELSE 0 END AS Value").SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1,comparisons);
        Assert.Equal("HOME","home".ToUpperInvariant());
        Assert.NotEqual("resume".ToUpperInvariant(),"résumé".ToUpperInvariant());
    }

    [Fact]
    public async Task RehearsalRepositoryWritesRoundTripInsideRolledBackTransaction()
    {
        var connectionString = Environment.GetEnvironmentVariable("TEMPLARCMS_SQL_REHEARSAL_CONNECTION");
        if (string.IsNullOrEmpty(connectionString)) Assert.Skip("Set TEMPLARCMS_SQL_REHEARSAL_CONNECTION for the opt-in SQL test.");
        Assert.StartsWith("templarcms_rehearsal_",new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString).InitialCatalog);
        await using var db = new TemplarCmsDbContext(new DbContextOptionsBuilder<TemplarCmsDbContext>().UseSqlServer(connectionString).Options);
        await using var transaction = await db.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
        try
        {
            var repository = new EfContentRepository(db);
            var id = new ContentItemId(Guid.NewGuid());
            var key = "sql-test-"+Guid.NewGuid().ToString("N");
            await repository.SaveItemAsync(new ContentItemDefinition(id,new string('n',255),new ContentItemKey(key),new TemplateId(Guid.NewGuid())),TestContext.Current.CancellationToken);
            var created = (await db.ContentItems.SingleAsync(i=>i.Id==id.Value,TestContext.Current.CancellationToken)).CreatedUtc;
            await repository.SaveFieldValuesAsync(id,[new ContentFieldValue(id,new FieldId(Guid.NewGuid()),new string('k',450),new ContentLanguage("en"),new ContentVersion(1),"value")],TestContext.Current.CancellationToken);
            db.ChangeTracker.Clear();
            var row = await db.ContentItems.SingleAsync(i=>i.Id==id.Value,TestContext.Current.CancellationToken);
            Assert.Equal(created,row.CreatedUtc);
            Assert.Equal(TimeSpan.Zero,row.ModifiedUtc.Offset);
            Assert.True(row.ModifiedUtc>=created);
            Assert.Equal("value",Assert.Single(await repository.GetFieldValuesAsync(id,TestContext.Current.CancellationToken)).Value);
            db.MediaAssets.Add(new PersistenceMediaAsset {Id=Guid.NewGuid(),FolderId=id.Value,FileName=new string('f',255),StoredFileName=new string('s',500),ContentType=new string('c',255),AltText=new string('a',500),Title=new string('t',255),CreatedUtc=DateTimeOffset.UtcNow,Length=1});
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            Assert.Equal(500,(await db.MediaAssets.SingleAsync(m=>m.FolderId==id.Value,TestContext.Current.CancellationToken)).AltText!.Length);
        }
        finally { await transaction.RollbackAsync(CancellationToken.None); }
    }
}
