using TemplarCMS.ContentModeling.Definitions;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Definitions;

public sealed class ContentItemDefinitionTests
{
    [Fact]
    public void Constructor_Throws_WhenIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new ContentItemDefinition(
                Guid.Empty,
                "Home",
                "home",
                Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_Throws_WhenTemplateIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new ContentItemDefinition(
                Guid.NewGuid(),
                "Home",
                "home",
                Guid.Empty));
    }

    [Fact]
    public void Constructor_TrimsNameAndKey()
    {
        var item = new ContentItemDefinition(
            Guid.NewGuid(),
            " Home ",
            " home ",
            Guid.NewGuid());

        Assert.Equal("Home", item.Name);
        Assert.Equal("home", item.Key);
    }

    [Fact]
    public void Constructor_AllowsParentId()
    {
        var parentId = Guid.NewGuid();

        var item = new ContentItemDefinition(
            Guid.NewGuid(),
            "Home",
            "home",
            Guid.NewGuid(),
            parentId);

        Assert.Equal(parentId, item.ParentId);
    }
}
