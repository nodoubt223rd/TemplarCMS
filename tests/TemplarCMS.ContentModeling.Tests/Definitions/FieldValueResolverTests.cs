using NSubstitute;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Definitions;

public sealed class FieldValueResolverTests
{
    [Fact]
    public void Constructor_Throws_WhenPolicyIsNull()
    {
        Assert.Throws<ArgumentNullException>(
            () => new FieldValueResolver(null!));
    }

    [Fact]
    public void Resolve_DelegatesToPolicy()
    {
        var policy = Substitute.For<IFieldValueResolutionPolicy>();

        var resolver = new FieldValueResolver(policy);

        var fieldDefinition = CreateFieldDefinition();

        var values = new List<ContentFieldValue>();

        var context = new FieldValueResolutionContext(
            new ContentLanguage("en"),
            ContentVersion.First);

        resolver.Resolve(
            fieldDefinition,
            values,
            context);

        policy.Received(1)
            .Resolve(
                fieldDefinition,
                values,
                context);
    }

    [Fact]
    public void Resolve_ReturnsResolvedValue()
    {
        var policy = Substitute.For<IFieldValueResolutionPolicy>();

        var resolver = new FieldValueResolver(policy);

        var fieldDefinition = CreateFieldDefinition();

        var values = new List<ContentFieldValue>();

        var context = new FieldValueResolutionContext(
            new ContentLanguage("en"),
            ContentVersion.First);

        var expected = new ContentFieldValue(
            new ContentItemId(Guid.NewGuid()),
            new FieldId(Guid.NewGuid()),
            "title",
            new ContentLanguage("en"),
            ContentVersion.First,
            "Home");

        policy.Resolve(
                fieldDefinition,
                values,
                context)
            .Returns(expected);

        var result = resolver.Resolve(
            fieldDefinition,
            values,
            context);

        Assert.Same(expected, result);
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenPolicyReturnsNull()
    {
        var policy = Substitute.For<IFieldValueResolutionPolicy>();

        var resolver = new FieldValueResolver(policy);

        var fieldDefinition = CreateFieldDefinition();

        var values = new List<ContentFieldValue>();

        var context = new FieldValueResolutionContext(
            new ContentLanguage("en"),
            ContentVersion.First);

        policy.Resolve(
                fieldDefinition,
                values,
                context)
            .Returns((ContentFieldValue?)null);

        var result = resolver.Resolve(
            fieldDefinition,
            values,
            context);

        Assert.Null(result);
    }

    private static FieldDefinition CreateFieldDefinition()
    {
        return new FieldDefinition(
            new FieldId(Guid.NewGuid()),
            "Title",
            "title",
            FieldType.SingleLineText);
    }
}
