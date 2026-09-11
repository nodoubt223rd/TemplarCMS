using Microsoft.EntityFrameworkCore;
using TemplarCMS.Persistence;
using Xunit;

namespace TemplarCMS.Integration.Tests.Persistence;

public sealed class SqlServerModelCompatibilityTests
{
    [Fact]
    public void Model_ShouldUseBoundedStringsForSqlServerIndexedColumns()
    {
        var options =
            new DbContextOptionsBuilder<TemplarCmsDbContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=TemplarCMS.ModelTest;Trusted_Connection=True;TrustServerCertificate=True")
                .Options;

        using var dbContext = new TemplarCmsDbContext(options);
        var contentItem = dbContext.Model.FindEntityType("TemplarCMS.Persistence.Content.PersistenceContentItem");
        var fieldValue = dbContext.Model.FindEntityType("TemplarCMS.Persistence.Content.PersistenceContentFieldValue");

        Assert.NotNull(contentItem);
        Assert.NotNull(fieldValue);
        Assert.Equal(450, contentItem.FindProperty("Key")?.GetMaxLength());
        Assert.Equal(450, fieldValue.FindProperty("Language")?.GetMaxLength());
    }
}
