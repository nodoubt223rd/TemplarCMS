using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Serialization;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Serialization;

public sealed class JsonTemplateMapperTests
{
    private readonly JsonTemplateMapper _mapper = new();

    [Fact]
    public void Map_ShouldMapTemplate()
    {
        var templateId = Guid.NewGuid();

        var jsonTemplate = new JsonTemplateDefinition
        {
            Id = templateId,
            Name = " Article Page ",
            Key = " article-page "
        };

        var result = _mapper.Map(jsonTemplate);

        Assert.Equal(new TemplateId(templateId), result.Id);
        Assert.Equal("Article Page", result.Name);
        Assert.Equal(new TemplateKey("article-page"), result.Key);
        Assert.Empty(result.Sections);
    }

    [Fact]
    public void Map_ShouldMapTemplateIcon()
    {
        var jsonTemplate = CreateTemplate();
        jsonTemplate.Icon = "layout";

        var result = _mapper.Map(jsonTemplate);

        Assert.Equal("layout", result.Icon);
    }

    [Fact]
    public void Map_ShouldMapSections()
    {
        var sectionId = Guid.NewGuid();

        var jsonTemplate = CreateTemplate();
        jsonTemplate.Sections.Add(new JsonTemplateSectionDefinition
        {
            Id = sectionId,
            Name = "Content",
            Key = "content",
            SortOrder = 100
        });

        var result = _mapper.Map(jsonTemplate);

        var section = Assert.Single(result.Sections);

        Assert.Equal(sectionId, section.Id);
        Assert.Equal("Content", section.Name);
        Assert.Equal("content", section.Key);
        Assert.Equal(100, section.SortOrder);
    }

    [Fact]
    public void Map_ShouldMapFields()
    {
        var fieldId = Guid.NewGuid();

        var jsonTemplate = CreateTemplateWithField(new JsonFieldDefinition
        {
            Id = fieldId,
            Name = "Title",
            Key = "title",
            FieldType = "singleLineText",
            IsShared = true,
            Metadata = new Dictionary<string, string>
            {
                ["maxLength"] = "100"
            }
        });

        var result = _mapper.Map(jsonTemplate);

        var section = Assert.Single(result.Sections);
        var field = Assert.Single(section.Fields);

        Assert.Equal(new FieldId(fieldId), field.Id);
        Assert.Equal("Title", field.Name);
        Assert.Equal("title", field.Key);
        Assert.Equal(FieldType.SingleLineText, field.FieldType);
        Assert.True(field.IsShared);
        Assert.False(field.IsUnversioned);
        Assert.Equal("100", field.Metadata["maxLength"]);
    }

    [Fact]
    public void Map_ShouldMapFieldId()
    {
        var fieldId = Guid.NewGuid();

        var jsonTemplate = CreateTemplateWithField(new JsonFieldDefinition
        {
            Id = fieldId,
            Name = "Title",
            Key = "title",
            FieldType = "singleLineText"
        });

        var result = _mapper.Map(jsonTemplate);

        var section = Assert.Single(result.Sections);
        var field = Assert.Single(section.Fields);

        Assert.Equal(new FieldId(fieldId), field.Id);
    }

    [Theory]
    [InlineData("singleLineText", FieldType.SingleLineText)]
    [InlineData("multiLineText", FieldType.MultiLineText)]
    [InlineData("richText", FieldType.RichText)]
    [InlineData(" RichText ", FieldType.RichText)]
    [InlineData("RICHTEXT", FieldType.RichText)]
    [InlineData("checkbox", FieldType.Checkbox)]
    [InlineData("dateTime", FieldType.DateTime)]
    [InlineData("integer", FieldType.Integer)]
    [InlineData("decimal", FieldType.Decimal)]
    [InlineData("droplink", FieldType.Droplink)]
    [InlineData("DropTree", FieldType.Droplink)]
    [InlineData("multilist", FieldType.Multilist)]
    [InlineData("Droplist", FieldType.Droplist)]
    [InlineData("choice", FieldType.Droplist)]
    [InlineData("TreeListEx", FieldType.Multilist)]
    [InlineData("Checklist", FieldType.Multilist)]
    [InlineData("Multilist with Search", FieldType.Multilist)]
    [InlineData("General Link", FieldType.GeneralLink)]
    [InlineData("image", FieldType.Image)]
    [InlineData("file", FieldType.File)]
    [InlineData("server file", FieldType.File)]
    [InlineData("json", FieldType.Json)]
    public void Map_ShouldMapFieldType(
        string jsonFieldType,
        FieldType expectedFieldType)
    {
        var jsonTemplate = CreateTemplateWithField(new JsonFieldDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Field",
            Key = "field",
            FieldType = jsonFieldType
        });

        var result = _mapper.Map(jsonTemplate);

        var section = Assert.Single(result.Sections);
        var field = Assert.Single(section.Fields);

        Assert.Equal(expectedFieldType, field.FieldType);
    }

    [Fact]
    public void Map_ShouldThrow_WhenTemplateIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _mapper.Map(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Map_ShouldThrow_WhenTemplateNameMissing(string? name)
    {
        var jsonTemplate = CreateTemplate();
        jsonTemplate.Name = name;

        Assert.Throws<InvalidOperationException>(() =>
            _mapper.Map(jsonTemplate));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Map_ShouldThrow_WhenTemplateKeyMissing(string? key)
    {
        var jsonTemplate = CreateTemplate();
        jsonTemplate.Key = key;

        Assert.Throws<InvalidOperationException>(() =>
            _mapper.Map(jsonTemplate));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Map_ShouldThrow_WhenFieldTypeMissing(string? fieldType)
    {
        var jsonTemplate = CreateTemplateWithField(new JsonFieldDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Title",
            Key = "title",
            FieldType = fieldType
        });

        Assert.Throws<InvalidOperationException>(() =>
            _mapper.Map(jsonTemplate));
    }

    [Fact]
    public void Map_ShouldThrow_WhenFieldTypeUnsupported()
    {
        var jsonTemplate = CreateTemplateWithField(new JsonFieldDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Title",
            Key = "title",
            FieldType = "unsupportedFieldType"
        });

        Assert.Throws<InvalidOperationException>(() =>
            _mapper.Map(jsonTemplate));
    }

    [Fact]
    public void Map_ShouldIncludeHint_WhenFieldTypeRequiresDedicatedSupport()
    {
        var jsonTemplate = CreateTemplateWithField(new JsonFieldDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Link",
            Key = "link",
            FieldType = "Version Link"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            _mapper.Map(jsonTemplate));

        Assert.Contains("version-aware link field type", exception.Message);
        Assert.Contains("Supported logical field types", exception.Message);
    }

    private static JsonTemplateDefinition CreateTemplate()
    {
        return new JsonTemplateDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Article Page",
            Key = "article-page"
        };
    }

    [Fact]
    public void Map_ShouldMapIsUnversioned()
    {
        var jsonTemplate = CreateTemplateWithField(new JsonFieldDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Title",
            Key = "title",
            FieldType = "singleLineText",
            IsUnversioned = true
        });

        var result = _mapper.Map(jsonTemplate);

        var section = Assert.Single(result.Sections);
        var field = Assert.Single(section.Fields);

        Assert.True(field.IsUnversioned);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Map_ShouldThrow_WhenSectionNameMissing(string? name)
    {
        var jsonTemplate = CreateTemplate();

        jsonTemplate.Sections.Add(new JsonTemplateSectionDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            Key = "content",
            SortOrder = 100
        });

        Assert.Throws<InvalidOperationException>(() =>
            _mapper.Map(jsonTemplate));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Map_ShouldThrow_WhenSectionKeyMissing(string? key)
    {
        var jsonTemplate = CreateTemplate();

        jsonTemplate.Sections.Add(new JsonTemplateSectionDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Content",
            Key = key,
            SortOrder = 100
        });

        Assert.Throws<InvalidOperationException>(() =>
            _mapper.Map(jsonTemplate));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Map_ShouldThrow_WhenFieldNameMissing(string? name)
    {
        var jsonTemplate = CreateTemplateWithField(new JsonFieldDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            Key = "title",
            FieldType = "singleLineText"
        });

        Assert.Throws<InvalidOperationException>(() =>
            _mapper.Map(jsonTemplate));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Map_ShouldThrow_WhenFieldKeyMissing(string? key)
    {
        var jsonTemplate = CreateTemplateWithField(new JsonFieldDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Title",
            Key = key,
            FieldType = "singleLineText"
        });

        Assert.Throws<InvalidOperationException>(() =>
            _mapper.Map(jsonTemplate));
    }

    private static JsonTemplateDefinition CreateTemplateWithField(
        JsonFieldDefinition field)
    {
        var template = CreateTemplate();

        template.Sections.Add(new JsonTemplateSectionDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Content",
            Key = "content",
            SortOrder = 100,
            Fields =
            [
                field
            ]
        });

        return template;
    }
}
