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
                new ContentItemId(Guid.Empty),
                "Home",
                new ContentItemKey("home"),
                new TemplateId(Guid.NewGuid())));
    }

    [Fact]
    public void Constructor_Throws_WhenTemplateIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new ContentItemDefinition(
                new ContentItemId(Guid.NewGuid()),
                "Home",
                new ContentItemKey("home"),
                new TemplateId(Guid.Empty)));
    }

    [Fact]
    public void Constructor_TrimsNameAndNormalizesContentItemKey()
    {
        var item = new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            " Home ",
            new ContentItemKey(" HOME PAGE "),
            new TemplateId(Guid.NewGuid()));

        Assert.Equal("Home", item.Name);
        Assert.Equal(new ContentItemKey("home-page"), item.Key);
    }

    [Fact]
    public void Constructor_AllowsParentId()
    {
        var parentId = new ContentItemId(Guid.NewGuid());

        var item = new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            "Home",
            new ContentItemKey("home"),
            new TemplateId(Guid.NewGuid()),
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

    [Fact]
    public void ContentItemId_Throws_WhenValueIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new ContentItemId(Guid.Empty));
    }

    [Fact]
    public void TemplateId_Throws_WhenValueIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new TemplateId(Guid.Empty));
    }
}
