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
    public void Constructor_Throws_WhenItemIsItsOwnParent()
    {
        var itemId = new ContentItemId(Guid.NewGuid());

        Assert.Throws<ArgumentException>(
            () => new ContentItemDefinition(
                itemId,
                "Home",
                new ContentItemKey("home"),
                new TemplateId(Guid.NewGuid()),
                itemId));
    }

    [Fact]
    public void IsRoot_ReturnsTrue_WhenParentIsMissing()
    {
        var item = new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            "Home",
            new ContentItemKey("home"),
            new TemplateId(Guid.NewGuid()));

        Assert.True(item.IsRoot);
        Assert.False(item.HasParent);
    }

    [Fact]
    public void IsRoot_ReturnsFalse_WhenParentExists()
    {
        var parentId = new ContentItemId(Guid.NewGuid());
        var item = new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            "Articles",
            new ContentItemKey("articles"),
            new TemplateId(Guid.NewGuid()),
            parentId);

        Assert.False(item.IsRoot);
        Assert.True(item.HasParent);
        Assert.True(item.IsDirectChildOf(parentId));
    }

    [Fact]
    public void UsesTemplate_ReturnsTrue_WhenTemplateMatches()
    {
        var templateId = new TemplateId(Guid.NewGuid());
        var item = new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            "Home",
            new ContentItemKey("home"),
            templateId);

        Assert.True(item.UsesTemplate(templateId));
        Assert.False(item.UsesTemplate(new TemplateId(Guid.NewGuid())));
    }

    [Fact]
    public void GetPath_ReturnsRootPath_ForRootItem()
    {
        var item = new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            "Home",
            new ContentItemKey("home"),
            new TemplateId(Guid.NewGuid()));

        var path = item.GetPath();

        Assert.Equal("/home", path.ToString());
    }

    [Fact]
    public void GetPath_AppendsKeyToParentPath_ForChildItem()
    {
        var item = new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            "Articles",
            new ContentItemKey("articles"),
            new TemplateId(Guid.NewGuid()),
            new ContentItemId(Guid.NewGuid()));

        var path = item.GetPath(new ContentPath("/home"));

        Assert.Equal("/home/articles", path.ToString());
    }

    [Fact]
    public void GetPath_Throws_WhenRootItemReceivesParentPath()
    {
        var item = new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            "Home",
            new ContentItemKey("home"),
            new TemplateId(Guid.NewGuid()));

        Assert.Throws<InvalidOperationException>(
            () => item.GetPath(new ContentPath("/root")));
    }

    [Fact]
    public void GetPath_Throws_WhenChildItemHasNoParentPath()
    {
        var item = new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            "Articles",
            new ContentItemKey("articles"),
            new TemplateId(Guid.NewGuid()),
            new ContentItemId(Guid.NewGuid()));

        Assert.Throws<InvalidOperationException>(
            () => item.GetPath());
    }

    [Fact]
    public void UpdateMetadata_ReturnsCopyWithUpdatedName()
    {
        var item = new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            "Home",
            new ContentItemKey("home"),
            new TemplateId(Guid.NewGuid()));

        var updated = item.UpdateMetadata(" Home Updated ");

        Assert.Equal(item.Id, updated.Id);
        Assert.Equal("Home Updated", updated.Name);
        Assert.Equal(item.Key, updated.Key);
        Assert.Equal(item.TemplateId, updated.TemplateId);
        Assert.Equal(item.ParentId, updated.ParentId);
    }

    [Fact]
    public void Rename_ReturnsCopyWithUpdatedNameAndKey()
    {
        var item = new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            "Home",
            new ContentItemKey("home"),
            new TemplateId(Guid.NewGuid()));

        var renamed = item.Rename(" Landing Page ", new ContentItemKey("landing page"));

        Assert.Equal(item.Id, renamed.Id);
        Assert.Equal("Landing Page", renamed.Name);
        Assert.Equal(new ContentItemKey("landing-page"), renamed.Key);
        Assert.Equal(item.TemplateId, renamed.TemplateId);
        Assert.Equal(item.ParentId, renamed.ParentId);
    }

    [Fact]
    public void MoveTo_ReturnsCopyWithUpdatedParent()
    {
        var originalParentId = new ContentItemId(Guid.NewGuid());
        var newParentId = new ContentItemId(Guid.NewGuid());
        var item = new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            "Home",
            new ContentItemKey("home"),
            new TemplateId(Guid.NewGuid()),
            originalParentId);

        var moved = item.MoveTo(newParentId);

        Assert.Equal(item.Id, moved.Id);
        Assert.Equal(item.Name, moved.Name);
        Assert.Equal(item.Key, moved.Key);
        Assert.Equal(item.TemplateId, moved.TemplateId);
        Assert.Equal(newParentId, moved.ParentId);
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
    public void ContentItemKey_StripsPunctuationIntoSeoFriendlySlug()
    {
        var key = ContentItemKey.FromDisplayName(" Hello, World's Best! ");

        Assert.Equal("hello-worlds-best", key.Value);
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
