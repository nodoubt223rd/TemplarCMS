using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Definitions;

public sealed class ExactMatchFieldValueResolutionPolicyTests
{
    private readonly ExactMatchFieldValueResolutionPolicy _policy = new();

    [Fact]
    public void Resolve_Throws_WhenFieldDefinitionIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _policy.Resolve(
                null!,
                [],
                CreateContext("en", ContentVersion.First)));
    }

    [Fact]
    public void Resolve_Throws_WhenValuesIsNull()
    {
        var field = CreateVersionedField();

        Assert.Throws<ArgumentNullException>(() =>
            _policy.Resolve(
                field,
                null!,
                CreateContext("en", ContentVersion.First)));
    }

    [Fact]
    public void Resolve_ReturnsSharedValue()
    {
        var field = CreateSharedField();

        var expected = CreateValue(
            new ContentLanguage("en"),
            ContentVersion.Shared);

        var result = _policy.Resolve(
            field,
            [expected],
            CreateContext("fr", ContentVersion.First));

        Assert.Same(expected, result);
    }

    [Fact]
    public void Resolve_ReturnsUnversionedValue()
    {
        var field = CreateUnversionedField();

        var expected = CreateValue(
            new ContentLanguage("en"),
            ContentVersion.Shared);

        var result = _policy.Resolve(
            field,
            [expected],
            CreateContext("en", ContentVersion.First));

        Assert.Same(expected, result);
    }

    [Fact]
    public void Resolve_ReturnsVersionedValue()
    {
        var field = CreateVersionedField();

        var expected = CreateValue(
            new ContentLanguage("en"),
            new ContentVersion(2));

        var result = _policy.Resolve(
            field,
            [expected],
            CreateContext("en", new ContentVersion(2)));

        Assert.Same(expected, result);
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenNoMatchExists()
    {
        var field = CreateVersionedField();

        var value = CreateValue(
            new ContentLanguage("en"),
            ContentVersion.First);

        var result = _policy.Resolve(
            field,
            [value],
            CreateContext("fr", ContentVersion.First));

        Assert.Null(result);
    }

    [Fact]
    public void Resolve_Throws_WhenContextIsNull()
    {
        var field = CreateVersionedField();

        Assert.Throws<ArgumentNullException>(() =>
            _policy.Resolve(
                field,
                [],
                null!));
    }

    private static ContentFieldValue CreateValue(ContentLanguage language, ContentVersion version)
    {
        return new ContentFieldValue(
            new ContentItemId(Guid.NewGuid()),
            new FieldId(Guid.NewGuid()),
            "title",
            language,
            version,
            "Home");
    }

    private static FieldDefinition CreateSharedField() =>
        new(new FieldId(Guid.NewGuid()), "Title", "title", FieldType.SingleLineText, isShared: true);

    private static FieldDefinition CreateUnversionedField() =>
        new(new FieldId(Guid.NewGuid()), "Title", "title", FieldType.SingleLineText, isUnversioned: true);

    private static FieldDefinition CreateVersionedField() =>
        new(new FieldId(Guid.NewGuid()), "Title", "title", FieldType.SingleLineText);

    private static FieldValueResolutionContext CreateContext(string language, ContentVersion version)
    {
        return new(
            new ContentLanguage(language),
            version);
    }
}
