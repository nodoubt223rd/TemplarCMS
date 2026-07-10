using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Validation;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Definitions;

public sealed class ContentItemResolverTests
{
    [Fact]
    public void Resolve_FiltersCandidateValuesByFieldIdBeforeDelegating()
    {
        var titleFieldId = new FieldId(Guid.NewGuid());
        var bodyFieldId = new FieldId(Guid.NewGuid());

        var titleField = CreateField(titleFieldId, "Title", "title");
        var bodyField = CreateField(bodyFieldId, "Body", "body");

        var template = new EffectiveTemplateDefinition(
            new TemplateId(Guid.NewGuid()),
            "Article Page",
            new TemplateKey("article-page"),
            [
                new TemplateSectionDefinition(
                    Guid.NewGuid(),
                    "Content",
                    "content",
                    fields: [titleField, bodyField])
            ]);

        var item = new ContentItemDefinition(
            new ContentItemId(Guid.NewGuid()),
            "Home",
            new ContentItemKey("home"),
            template.Id);

        var titleValue = CreateValue(item.Id, titleFieldId, "title", "Home");
        var bodyValue = CreateValue(item.Id, bodyFieldId, "body", "Welcome");
        var unrelatedValue = CreateValue(item.Id, new FieldId(Guid.NewGuid()), "summary", "Ignore me");

        var fieldValueResolver = new RecordingFieldValueResolver();
        var typedFieldValueConverter = new PassThroughTypedFieldValueConverter();
        var resolver = new ContentItemResolver(fieldValueResolver, typedFieldValueConverter);

        var context = new FieldValueResolutionContext(
            new ContentLanguage("en"),
            ContentVersion.First);

        var result = resolver.Resolve(
            item,
            template,
            [titleValue, bodyValue, unrelatedValue],
            context);

        Assert.Equal(2, fieldValueResolver.Calls.Count);

        var titleCall =
            Assert.Single(
                fieldValueResolver.Calls.Where(call => call.FieldId == titleFieldId));

        Assert.Equal([titleFieldId], titleCall.CandidateFieldIds);

        var bodyCall =
            Assert.Single(
                fieldValueResolver.Calls.Where(call => call.FieldId == bodyFieldId));

        Assert.Equal([bodyFieldId], bodyCall.CandidateFieldIds);

        Assert.Equal("Home", result.Fields["title"]?.Value);
        Assert.Equal("Welcome", result.Fields["body"]?.Value);
    }

    private static FieldDefinition CreateField(
        FieldId id,
        string name,
        string key)
    {
        return new FieldDefinition(
            id,
            name,
            key,
            FieldType.SingleLineText);
    }

    private static ContentFieldValue CreateValue(
        ContentItemId itemId,
        FieldId fieldId,
        string fieldKey,
        string? value)
    {
        return new ContentFieldValue(
            itemId,
            fieldId,
            fieldKey,
            new ContentLanguage("en"),
            ContentVersion.First,
            value);
    }

    private sealed class RecordingFieldValueResolver : IFieldValueResolver
    {
        public List<ResolverCall> Calls { get; } = [];

        public ContentFieldValue? Resolve(
            FieldDefinition fieldDefinition,
            IReadOnlyCollection<ContentFieldValue> values,
            FieldValueResolutionContext context)
        {
            Calls.Add(
                new ResolverCall(
                    fieldDefinition.Id,
                    [.. values.Select(value => value.FieldId)]));

            return values.FirstOrDefault();
        }
    }

    private sealed class PassThroughTypedFieldValueConverter : ITypedFieldValueConverter
    {
        public ValidationResult<ConvertedFieldValue> Convert(
            FieldDefinition fieldDefinition,
            ContentFieldValue? value)
        {
            TypedFieldValue typedValue =
                value == null || value.Value == null
                    ? new NullTypedFieldValue()
                    : new StringTypedFieldValue(value.Value);

            return new ValidationResult<ConvertedFieldValue>(
                new ConvertedFieldValue(
                    fieldDefinition,
                    value,
                    typedValue));
        }
    }

    private sealed record ResolverCall(
        FieldId FieldId,
        IReadOnlyList<FieldId> CandidateFieldIds);
}
