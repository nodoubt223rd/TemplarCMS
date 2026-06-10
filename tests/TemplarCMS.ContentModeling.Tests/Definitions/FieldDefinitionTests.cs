using TemplarCMS.ContentModeling.Definitions;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Definitions;

public sealed class FieldDefinitionTests
{
    [Fact]
    public void Constructor_Throws_WhenIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new FieldDefinition(
                Guid.Empty,
                "Title",
                "title",
                FieldType.SingleLineText));
    }

    [Fact]
    public void Constructor_Throws_WhenNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new FieldDefinition(
                Guid.NewGuid(),
                string.Empty,
                "title",
                FieldType.SingleLineText));
    }

    [Fact]
    public void Constructor_Throws_WhenKeyIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new FieldDefinition(
                Guid.NewGuid(),
                "Title",
                string.Empty,
                FieldType.SingleLineText));
    }

    [Fact]
    public void Constructor_TrimsNameAndKey()
    {
        var field = new FieldDefinition(
            Guid.NewGuid(),
            " Title ",
            " title ",
            FieldType.SingleLineText);

        Assert.Equal("Title", field.Name);
        Assert.Equal("title", field.Key);
    }

    [Fact]
    public void Constructor_CopiesMetadata()
    {
        var metadata = new Dictionary<string, string>
        {
            ["maxLength"] = "100"
        };

        var field = new FieldDefinition(
            Guid.NewGuid(),
            "Title",
            "title",
            FieldType.SingleLineText,
            metadata: metadata);

        metadata["maxLength"] = "200";

        Assert.Equal("100", field.Metadata["maxLength"]);
    }

    [Fact]
    public void IsVersioned_ReturnsFalse_WhenFieldIsShared()
    {
        var field = new FieldDefinition(
            Guid.NewGuid(),
            "Title",
            "title",
            FieldType.SingleLineText,
            isShared: true);

        Assert.False(field.IsVersioned);
    }

    [Fact]
    public void IsVersioned_ReturnsFalse_WhenFieldIsUnversioned()
    {
        var field = new FieldDefinition(
            Guid.NewGuid(),
            "Title",
            "title",
            FieldType.SingleLineText,
            isUnversioned: true);

        Assert.False(field.IsVersioned);
    }

    [Fact]
    public void IsVersioned_ReturnsTrue_WhenFieldIsNeitherSharedNorUnversioned()
    {
        var field = new FieldDefinition(
            Guid.NewGuid(),
            "Title",
            "title",
            FieldType.SingleLineText);

        Assert.True(field.IsVersioned);
    }

    [Fact]
    public void ValueScope_ReturnsShared_WhenFieldIsShared()
    {
        var field = new FieldDefinition(
            Guid.NewGuid(),
            "Title",
            "title",
            FieldType.SingleLineText,
            isShared: true);

        Assert.Equal(FieldValueScope.Shared, field.ValueScope);
    }

    [Fact]
    public void ValueScope_ReturnsUnversioned_WhenFieldIsUnversioned()
    {
        var field = new FieldDefinition(
            Guid.NewGuid(),
            "Title",
            "title",
            FieldType.SingleLineText,
            isUnversioned: true);

        Assert.Equal(FieldValueScope.Unversioned, field.ValueScope);
    }

    [Fact]
    public void ValueScope_ReturnsVersioned_WhenFieldIsNeitherSharedNorUnversioned()
    {
        var field = new FieldDefinition(
            Guid.NewGuid(),
            "Title",
            "title",
            FieldType.SingleLineText);

        Assert.Equal(FieldValueScope.Versioned, field.ValueScope);
    }
}
