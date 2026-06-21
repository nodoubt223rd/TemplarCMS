using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Definitions;

public sealed class EffectiveTemplateDefinitionTests
{
    [Fact]
    public void Constructor_PreservesSectionOrder()
    {
        var firstSection = CreateSection(
            "First Section",
            "first-section",
            200,
            CreateField("First Field", "first-field"));

        var secondSection = CreateSection(
            "Second Section",
            "second-section",
            100,
            CreateField("Second Field", "second-field"));

        var template = new EffectiveTemplateDefinition(
            Guid.NewGuid(),
            "Article Page",
            "article-page",
            [firstSection, secondSection]);

        Assert.Collection(
            template.Sections,
            section => Assert.Equal("first-section", section.Key),
            section => Assert.Equal("second-section", section.Key));
    }

    [Fact]
    public void Constructor_FlattensFieldsInSectionOrder()
    {
        var seoSection = CreateSection(
            "SEO",
            "seo",
            100,
            CreateField("Meta Title", "meta-title"),
            CreateField("Meta Description", "meta-description"));

        var contentSection = CreateSection(
            "Content",
            "content",
            200,
            CreateField("Title", "title"),
            CreateField("Body", "body"));

        var template = new EffectiveTemplateDefinition(
            Guid.NewGuid(),
            "Article Page",
            "article-page",
            [seoSection, contentSection]);

        Assert.Collection(
            template.Fields,
            field => Assert.Equal("meta-title", field.Key),
            field => Assert.Equal("meta-description", field.Key),
            field => Assert.Equal("title", field.Key),
            field => Assert.Equal("body", field.Key));
    }

    [Fact]
    public void Constructor_ReturnsEmptySectionsAndFields_WhenSectionsAreNull()
    {
        var template = new EffectiveTemplateDefinition(
            Guid.NewGuid(),
            "Article Page",
            "article-page");

        Assert.Empty(template.Sections);
        Assert.Empty(template.Fields);
    }

    [Fact]
    public void Constructor_ReturnsEmptyFields_WhenSectionsContainNoFields()
    {
        var section = CreateSection(
            "Content",
            "content",
            100);

        var template = new EffectiveTemplateDefinition(
            Guid.NewGuid(),
            "Article Page",
            "article-page",
            [section]);

        var effectiveSection = Assert.Single(template.Sections);
        Assert.Equal("content", effectiveSection.Key);
        Assert.Empty(template.Fields);
    }

    [Fact]
    public void Constructor_CopiesSectionsDefensively()
    {
        var section = CreateSection(
            "Content",
            "content",
            100,
            CreateField("Title", "title"));

        var sections = new List<TemplateSectionDefinition>
        {
            section
        };

        var template = new EffectiveTemplateDefinition(
            Guid.NewGuid(),
            "Article Page",
            "article-page",
            sections);

        sections.Clear();

        var effectiveSection = Assert.Single(template.Sections);
        Assert.Equal("content", effectiveSection.Key);

        var effectiveField = Assert.Single(template.Fields);
        Assert.Equal("title", effectiveField.Key);
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
