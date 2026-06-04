using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Validation;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Validation;

public sealed class TemplateValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ReturnsValid_ForValidTemplate()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate(
            "Article Page",
            "article-page",
            CreateSection(
                "Content",
                "content",
                CreateField("Title", "title")));

        var validator = new TemplateValidator();

        var result = await validator.ValidateAsync(template, cancellationToken);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsTemplateRequired_WhenTemplateIsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var validator = new TemplateValidator();

        var result = await validator.ValidateAsync(null!, cancellationToken);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal("TemplateRequired", error.Code);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsDuplicateSectionKey_WhenTemplateContainsDuplicateSectionKeys()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate(
            "Article Page",
            "article-page",
            CreateSection("Content", "content"),
            CreateSection("Main Content", "content"));

        var validator = new TemplateValidator();

        var result = await validator.ValidateAsync(template, cancellationToken);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal("DuplicateSectionKey", error.Code);
        Assert.Equal("content", error.Target);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsDuplicateSectionKey_WhenSectionKeysDifferOnlyByCasing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate(
            "Article Page",
            "article-page",
            CreateSection("Content", "content"),
            CreateSection("Main Content", "CONTENT"));

        var validator = new TemplateValidator();

        var result = await validator.ValidateAsync(template, cancellationToken);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal("DuplicateSectionKey", error.Code);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsDuplicateFieldKeyInSection_WhenSectionContainsDuplicateFieldKeys()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate(
            "Article Page",
            "article-page",
            CreateSection(
                "Content",
                "content",
                CreateField("Title", "title"),
                CreateField("Heading", "title")));

        var validator = new TemplateValidator();

        var result = await validator.ValidateAsync(template, cancellationToken);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error =>
                error.Code == "DuplicateFieldKeyInSection" &&
                error.Target == "content.title");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsDuplicateFieldKeyInSection_WhenFieldKeysDifferOnlyByCasing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate(
            "Article Page",
            "article-page",
            CreateSection(
                "Content",
                "content",
                CreateField("Title", "title"),
                CreateField("Heading", "TITLE")));

        var validator = new TemplateValidator();

        var result = await validator.ValidateAsync(template, cancellationToken);

        Assert.False(result.IsValid);

        Assert.Contains(
            result.Errors,
            error => error.Code == "DuplicateFieldKeyInSection");
    }

    [Fact]
    public async Task ValidateAsync_ReturnsDuplicateFieldKeyInTemplate_WhenTemplateContainsDuplicateFieldKeysAcrossSections()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate(
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

        var validator = new TemplateValidator();

        var result = await validator.ValidateAsync(template, cancellationToken);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal("DuplicateFieldKeyInTemplate", error.Code);
        Assert.Equal("title", error.Target);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsDuplicateFieldKeyInTemplate_WhenFieldKeysAcrossSectionsDifferOnlyByCasing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate(
            "Article Page",
            "article-page",
            CreateSection(
                "Content",
                "content",
                CreateField("Title", "title")),
            CreateSection(
                "SEO",
                "seo",
                CreateField("Meta Title", "TITLE")));

        var validator = new TemplateValidator();

        var result = await validator.ValidateAsync(template, cancellationToken);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal("DuplicateFieldKeyInTemplate", error.Code);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsSectionFieldKeyCollision_WhenSectionAndFieldShareKey()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate(
            "Article Page",
            "article-page",
            CreateSection(
                "Content",
                "content",
                CreateField("Content", "content")));

        var validator = new TemplateValidator();

        var result = await validator.ValidateAsync(template, cancellationToken);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal("SectionFieldKeyCollision", error.Code);
        Assert.Equal("content", error.Target);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsSectionFieldKeyCollision_WhenKeysDifferOnlyByCasing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate(
            "Article Page",
            "article-page",
            CreateSection(
                "Content",
                "content",
                CreateField("Content", "CONTENT")));

        var validator = new TemplateValidator();

        var result = await validator.ValidateAsync(template, cancellationToken);

        Assert.False(result.IsValid);

        var error = Assert.Single(result.Errors);
        Assert.Equal("SectionFieldKeyCollision", error.Code);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsMultipleErrors_WhenTemplateContainsMultipleValidationIssues()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate(
            "Article Page",
            "article-page",
            CreateSection(
                "Content",
                "content",
                CreateField("Title", "title"),
                CreateField("Heading", "title")),
            CreateSection(
                "Main Content",
                "content",
                CreateField("Content", "content")));

        var validator = new TemplateValidator();

        var result = await validator.ValidateAsync(template, cancellationToken);

        Assert.False(result.IsValid);

        Assert.Contains(result.Errors, error => error.Code == "DuplicateSectionKey");
        Assert.Contains(result.Errors, error => error.Code == "DuplicateFieldKeyInSection");
        Assert.Contains(result.Errors, error => error.Code == "SectionFieldKeyCollision");
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrow_WhenCancellationRequested()
    {
        var template = CreateTemplate(
            "Article Page",
            "article-page");

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        var validator = new TemplateValidator();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            validator.ValidateAsync(
                template,
                cancellationTokenSource.Token));
    }

    private static TemplateDefinition CreateTemplate(
        string name,
        string key,
        params TemplateSectionDefinition[] sections)
    {
        return new TemplateDefinition(
            Guid.NewGuid(),
            name,
            key,
            sections: sections);
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
            Guid.NewGuid(),
            name,
            key,
            fieldType);
    }
}
