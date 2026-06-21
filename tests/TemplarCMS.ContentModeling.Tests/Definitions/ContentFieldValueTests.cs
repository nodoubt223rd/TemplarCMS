using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Definitions;

public sealed class ContentFieldValueTests
{
    [Fact]
    public void Constructor_Throws_WhenItemIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new ContentFieldValue(
                Guid.Empty,
                Guid.NewGuid(),
                "title",
                new ContentLanguage("en"),
                ContentVersion.First,
                "Home"));
    }

    [Fact]
    public void Constructor_Throws_WhenFieldIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new ContentFieldValue(
                Guid.NewGuid(),
                Guid.Empty,
                "title",
                new ContentLanguage("en"),
                ContentVersion.First,
                "Home"));
    }

    [Fact]
    public void Constructor_Throws_WhenFieldKeyIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new ContentFieldValue(
                Guid.NewGuid(),
                Guid.NewGuid(),
                string.Empty,
                new ContentLanguage("en"),
                ContentVersion.First,
                "Home"));
    }

    [Fact]
    public void Constructor_TrimsFieldKey()
    {
        var value = new ContentFieldValue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            " title ",
            new ContentLanguage("en"),
            ContentVersion.First,
            "Home");

        Assert.Equal("title", value.FieldKey);
    }

    [Fact]
    public void Constructor_PreservesLanguageAndVersion()
    {
        var language = new ContentLanguage("en-us");
        var version = new ContentVersion(2);

        var value = new ContentFieldValue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "title",
            language,
            version,
            "Home");

        Assert.Equal(language, value.Language);
        Assert.Equal(version, value.Version);
    }

    [Fact]
    public void Constructor_AllowsNullValue()
    {
        var value = new ContentFieldValue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "title",
            new ContentLanguage("en"),
            ContentVersion.First,
            null);

        Assert.Null(value.Value);
    }
}
