using NSubstitute;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.ContentModeling.Builders;
using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Resolvers;
using TemplarCMS.ContentModeling.Validation;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Builders;

public sealed class EffectiveTemplateBuilderTests
{
    [Fact]
    public async Task BuildEffectiveTemplateAsync_ReturnsEffectiveTemplate_ForSimpleTemplate()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var section = CreateSection(
            "Content",
            "content",
            100,
            CreateField("Title", "title"));

        var template = CreateTemplate(
            "Article",
            "article",
            sections: new[] { section });

        var builder = CreateBuilder();

        var result = await builder.BuildEffectiveTemplateAsync(template, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(template.Id, result.Value!.Id);
        Assert.Equal("Article", result.Value.Name);
        Assert.Equal("article", result.Value.Key);

        var effectiveSection = Assert.Single(result.Value.Sections);
        Assert.Equal("Content", effectiveSection.Name);

        var effectiveField = Assert.Single(effectiveSection.Fields);
        Assert.Equal("title", effectiveField.Key);
    }

    [Fact]
    public async Task BuildEffectiveTemplateAsync_IncludesInheritedSections_ForSingleInheritance()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seoSection = CreateSection(
            "SEO",
            "seo",
            100,
            CreateField("Meta Title", "meta-title"));

        var contentSection = CreateSection(
            "Content",
            "content",
            200,
            CreateField("Title", "title"));

        var baseTemplate = CreateTemplate(
            "Base Page",
            "base-page",
            sections: [seoSection]);

        var articleTemplate = CreateTemplate(
            "Article Page",
            "article-page",
            baseTemplate: baseTemplate,
            sections: [contentSection]);

        var builder = CreateBuilder();

        var result = await builder.BuildEffectiveTemplateAsync(articleTemplate, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Collection(
            result.Value!.Sections,
            section => Assert.Equal("seo", section.Key),
            section => Assert.Equal("content", section.Key));
    }

    [Fact]
    public async Task BuildEffectiveTemplateAsync_MergesSections_WithSameKey()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var baseContentSection = CreateSection(
            "Content",
            "content",
            100,
            CreateField("Title", "title"));

        var derivedContentSection = CreateSection(
            "Content",
            "content",
            100,
            CreateField("Summary", "summary"));

        var baseTemplate = CreateTemplate(
            "Base Page",
            "base-page",
            sections: [baseContentSection]);

        var articleTemplate = CreateTemplate(
            "Article Page",
            "article-page",
            baseTemplate: baseTemplate,
            sections: [derivedContentSection]);

        var builder = CreateBuilder();

        var result = await builder.BuildEffectiveTemplateAsync(articleTemplate, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);

        var section = Assert.Single(result.Value!.Sections);

        Assert.Equal("content", section.Key);
        Assert.Collection(
            section.Fields,
            field => Assert.Equal("title", field.Key),
            field => Assert.Equal("summary", field.Key));
    }

    [Fact]
    public async Task BuildEffectiveTemplateAsync_AppliesDerivedSectionOverride_WhenSectionKeyMatches()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var baseSection = CreateSection(
            "Base Content",
            "content",
            100,
            CreateField("Title", "title"));

        var derivedSection = CreateSection(
            "Article Content",
            "content",
            200,
            CreateField("Summary", "summary"));

        var baseTemplate = CreateTemplate(
            "Base Page",
            "base-page",
            sections: [baseSection]);

        var articleTemplate = CreateTemplate(
            "Article Page",
            "article-page",
            baseTemplate: baseTemplate,
            sections: [derivedSection]);

        var builder = CreateBuilder();

        var result = await builder.BuildEffectiveTemplateAsync(articleTemplate, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);

        var section = Assert.Single(result.Value!.Sections);

        Assert.Equal(derivedSection.Id, section.Id);
        Assert.Equal("Article Content", section.Name);
        Assert.Equal("content", section.Key);
        Assert.Equal(200, section.SortOrder);
    }

    [Fact]
    public async Task BuildEffectiveTemplateAsync_AppliesDerivedFieldOverride_WhenFieldKeyMatches()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var baseTitleField = new FieldDefinition(
            new FieldId(Guid.NewGuid()),
            "Base Title",
            "title",
            FieldType.SingleLineText);

        var derivedTitleField = new FieldDefinition(
            new FieldId(Guid.NewGuid()),
            "Article Title",
            "title",
            FieldType.RichText);

        var baseSection = CreateSection(
            "Content",
            "content",
            100,
            baseTitleField);

        var derivedSection = CreateSection(
            "Content",
            "content",
            100,
            derivedTitleField);

        var baseTemplate = CreateTemplate(
            "Base Page",
            "base-page",
            sections: [baseSection]);

        var articleTemplate = CreateTemplate(
            "Article Page",
            "article-page",
            baseTemplate: baseTemplate,
            sections: [derivedSection]);

        var builder = CreateBuilder();

        var result = await builder.BuildEffectiveTemplateAsync(articleTemplate, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);

        var section = Assert.Single(result.Value!.Sections);
        var field = Assert.Single(section.Fields);

        Assert.Equal("Article Title", field.Name);
        Assert.Equal("title", field.Key);
        Assert.Equal(FieldType.RichText, field.FieldType);
    }

    [Fact]
    public async Task BuildEffectiveTemplateAsync_MergesFieldsAcrossDeepInheritanceChain()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var baseContentSection = CreateSection(
            "SEO",
            "seo",
            100,
            CreateField("Meta Title", "meta-title"));

        var basePageSection = CreateSection(
            "SEO",
            "seo",
            100,
            CreateField("Meta Description", "meta-description"));

        var articleSection = CreateSection(
            "Content",
            "content",
            200,
            CreateField("Title", "title"));

        var baseContent = CreateTemplate(
            "Base Content",
            "base-content",
            sections: [baseContentSection]);

        var basePage = CreateTemplate(
            "Base Page",
            "base-page",
            baseTemplate: baseContent,
            sections: [basePageSection]);

        var articleTemplate = CreateTemplate(
            "Article Page",
            "article-page",
            baseTemplate: basePage,
            sections: [articleSection]);

        var builder = CreateBuilder();

        var result = await builder.BuildEffectiveTemplateAsync(articleTemplate, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);

        Assert.Collection(
            result.Value!.Sections,
            section =>
            {
                Assert.Equal("seo", section.Key);
                Assert.Collection(
                    section.Fields,
                    field => Assert.Equal("meta-title", field.Key),
                    field => Assert.Equal("meta-description", field.Key));
            },
            section =>
            {
                Assert.Equal("content", section.Key);
                var field = Assert.Single(section.Fields);
                Assert.Equal("title", field.Key);
            });
    }

    [Fact]
    public async Task BuildEffectiveTemplateAsync_AppliesOverrides_CaseInsensitively()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var baseTitleField = new FieldDefinition(
            new FieldId(Guid.NewGuid()),
            "Base Title",
            "title",
            FieldType.SingleLineText);

        var derivedTitleField = new FieldDefinition(
            new FieldId(Guid.NewGuid()),
            "Article Title",
            "Title",
            FieldType.RichText);

        var baseSection = CreateSection(
            "Base Content",
            "content",
            100,
            baseTitleField);

        var derivedSection = CreateSection(
            "Article Content",
            "Content",
            50,
            derivedTitleField);

        var baseTemplate = CreateTemplate(
            "Base Page",
            "base-page",
            sections: [baseSection]);

        var articleTemplate = CreateTemplate(
            "Article Page",
            "article-page",
            baseTemplate: baseTemplate,
            sections: [derivedSection]);

        var builder = CreateBuilder();

        var result = await builder.BuildEffectiveTemplateAsync(articleTemplate, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);

        var section = Assert.Single(result.Value!.Sections);
        Assert.Equal(derivedSection.Id, section.Id);
        Assert.Equal("Article Content", section.Name);
        Assert.Equal("content", section.Key);
        Assert.Equal(50, section.SortOrder);

        var field = Assert.Single(section.Fields);
        Assert.Equal(derivedTitleField.Id, field.Id);
        Assert.Equal("Article Title", field.Name);
        Assert.Equal("Title", field.Key);
        Assert.Equal(FieldType.RichText, field.FieldType);
    }

    [Fact]
    public async Task BuildEffectiveTemplateAsync_ReturnsErrors_WhenInheritanceResolutionFails()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var template = CreateTemplate("Article", "article");

        var error = new ValidationError(
            "CircularTemplateInheritance",
            "Circular template inheritance detected.",
            "article");

        var inheritanceResolver = Substitute.For<ITemplateInheritanceResolver>();
        inheritanceResolver
            .ResolveAsync(template, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult<InheritedTemplateDefinition>(
                null,
                [error]));

        var builder = new EffectiveTemplateBuilder(inheritanceResolver);

        var result = await builder.BuildEffectiveTemplateAsync(template, cancellationToken);

        Assert.False(result.IsValid);
        Assert.False(result.HasValue);

        var resultError = Assert.Single(result.Errors);
        Assert.Equal("CircularTemplateInheritance", resultError.Code);
    }

    private static EffectiveTemplateBuilder CreateBuilder()
    {
        return new EffectiveTemplateBuilder(new TemplateInheritanceResolver());
    }

    private static TemplateDefinition CreateTemplate(
        string name,
        string key,
        TemplateDefinition? baseTemplate = null,
        IReadOnlyCollection<TemplateSectionDefinition>? sections = null)
    {
        return new TemplateDefinition(
            Guid.NewGuid(),
            name,
            key,
            baseTemplate,
            sections);
    }

    private static TemplateSectionDefinition CreateSection(
        string name,
        string key,
        int sortOrder,
        params FieldDefinition[] fields)
    {
        return new TemplateSectionDefinition(
            Guid.NewGuid(),
            name,
            key,
            sortOrder,
            fields);
    }

    private static FieldDefinition CreateField(
        string name,
        string key,
        FieldType fieldType = FieldType.SingleLineText)
    {
        return new FieldDefinition(
            new FieldId(Guid.NewGuid()),
            name,
            key,
            fieldType);
    }
}
