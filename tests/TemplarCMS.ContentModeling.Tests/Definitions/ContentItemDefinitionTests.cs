using TemplarCMS.Domain.Content;
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
                new ContentItemKey("home"),
                Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_Throws_WhenTemplateIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new ContentItemDefinition(
                Guid.NewGuid(),
                "Home",
                new ContentItemKey("home"),
                Guid.Empty));
    }

    [Fact]
    public void Constructor_TrimsNameAndNormalizesContentItemKey()
    {
        var item = new ContentItemDefinition(
            Guid.NewGuid(),
            " Home ",
            new ContentItemKey(" HOME PAGE "),
            Guid.NewGuid());

        Assert.Equal("Home", item.Name);
        Assert.Equal(new ContentItemKey("home-page"), item.Key);
    }

    [Fact]
    public void Constructor_AllowsParentId()
    {
        var parentId = Guid.NewGuid();

        var item = new ContentItemDefinition(
            Guid.NewGuid(),
            "Home",
            new ContentItemKey("home"),
            Guid.NewGuid(),
            parentId);

        Assert.Equal(parentId, item.ParentId);
    }

    [Fact]
    public void Constructor_Throws_WhenContentItemKeyIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new ContentItemKey(" "));
    }

    [Fact]
    public void ContentItemKey_ComparesCaseInsensitively()
    {
        Assert.Equal(
            new ContentItemKey("home"),
            new ContentItemKey("HOME"));
    }

    [Fact]
    public void ContentItemKey_NormalizesValueToLowercase()
    {
        var key = new ContentItemKey(" HOME PAGE ");

        Assert.Equal("home-page", key.Value);
        Assert.Equal("home-page", key.ToString());
    }

    [Fact]
    public void ContentItemKey_ConvertsWhitespaceRunsToSingleHyphens()
    {
        var key = new ContentItemKey("  New   Campaign  ");

        Assert.Equal("new-campaign", key.Value);
    }
}
