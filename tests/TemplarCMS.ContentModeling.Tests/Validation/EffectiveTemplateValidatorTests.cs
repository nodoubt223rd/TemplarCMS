using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Validation;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Validation;

public sealed class EffectiveTemplateValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ReturnsValid_ForValidEffectiveTemplate()
    {
        var template = CreateEffectiveTemplate(
            "Article Page",
            "article-page",
            CreateSection(
                "Content",
                "content",
                CreateField("Title", "title")),
            CreateSection(
                "SEO",
                "seo",
                CreateField("Meta Title", "meta-title")));

        var validator = new EffectiveTemplateValidator();

        var result = await validator.ValidateAsync(template);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsEffectiveTemplateRequired_WhenTemplateIsNull()
    {
        var validator = new EffectiveTemplateValidator();

        var result = await validator.ValidateAsync(null!);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal("EffectiveTemplateRequired", error.Code);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsDuplicateEffectiveSectionKey_WhenSectionsShareKey()
    {
        var template = CreateEffectiveTemplate(
            "Article Page",
            "article-page",
            CreateSection("Content", "content"),
            CreateSection("Main Content", "content"));

        var validator = new EffectiveTemplateValidator();

        var result = await validator.ValidateAsync(template);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal("DuplicateEffectiveSectionKey", error.Code);
        Assert.Equal("content", error.Target);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsDuplicateEffectiveFieldKey_WhenFieldsShareKey()
    {
        var template = CreateEffectiveTemplate(
            "Article Page",
            "article-page",
            CreateSection(
                "Content",
                "content",
                CreateField("Title", "title")),
            CreateSection(
                "SEO",
                "seo",
                CreateField("Meta Title", "title")));

        var validator = new EffectiveTemplateValidator();

        var result = await validator.ValidateAsync(template);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal("DuplicateEffectiveFieldKey", error.Code);
        Assert.Equal("title", error.Target);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsEffectiveSectionFieldKeyCollision_WhenSectionAndFieldShareKey()
    {
        var template = CreateEffectiveTemplate(
            "Article Page",
            "article-page",
            CreateSection(
                "Content",
                "content",
                CreateField("Content", "content")));

        var validator = new EffectiveTemplateValidator();

        var result = await validator.ValidateAsync(template);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal("EffectiveSectionFieldKeyCollision", error.Code);
        Assert.Equal("content", error.Target);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsMultipleErrors_WhenEffectiveTemplateContainsMultipleIssues()
    {
        var template = CreateEffectiveTemplate(
            "Article Page",
            "article-page",
            CreateSection(
                "Content",
                "content",
                CreateField("Title", "title")),
            CreateSection(
                "Main Content",
                "content",
                CreateField("Content", "content")),
            CreateSection(
                "SEO",
                "seo",
                CreateField("Meta Title", "title")));

        var validator = new EffectiveTemplateValidator();

        var result = await validator.ValidateAsync(template);

        Assert.False(result.IsValid);

        Assert.Contains(result.Errors, error => error.Code == "DuplicateEffectiveSectionKey");
        Assert.Contains(result.Errors, error => error.Code == "DuplicateEffectiveFieldKey");
        Assert.Contains(result.Errors, error => error.Code == "EffectiveSectionFieldKeyCollision");
    }

    private static EffectiveTemplateDefinition CreateEffectiveTemplate(
        string name,
        string key,
        params TemplateSectionDefinition[] sections)
    {
        return new EffectiveTemplateDefinition(
            Guid.NewGuid(),
            name,
            key,
            sections);
    }

    private static TemplateSectionDefinition CreateSection(
        string name,
        string key,
        params FieldDefinition[] fields)
    {
        return new TemplateSectionDefinition(
            Guid.NewGuid(),
            name,
            key,
            fields: fields);
    }

    private static FieldDefinition CreateField(
        string name,
        string key,
        FieldType fieldType = FieldType.SingleLineText)
    {
        return new FieldDefinition(
            name,
            key,
            fieldType);
    }
}
