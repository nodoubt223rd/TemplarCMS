using TemplarCMS.ContentModeling.Definitions;
using TemplarCMS.ContentModeling.Abstractions;
using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Definitions;

public sealed class TypedFieldValueConverterTests
{
    private readonly TypedFieldValueConverter _converter = new();

    [Fact]
    public void Convert_ReturnsStringValue_ForSingleLineText()
    {
        var field = CreateField(FieldType.SingleLineText, "title");
        var value = CreateValue("Home", "title");

        var result = _converter.Convert(field, value);

        Assert.True(result.Succeeded);
        var converted = Assert.IsType<StringTypedFieldValue>(result.Value!.Value);
        Assert.Equal("Home", converted.Value);
    }

    [Fact]
    public void Convert_ReturnsStringValue_ForGeneralLink()
    {
        var field = CreateField(FieldType.GeneralLink, "help-link");
        var value = CreateValue(
            "{\"kind\":\"external\",\"url\":\"https://templarcms.dev/help\",\"text\":\"Help\"}",
            "help-link");

        var result = _converter.Convert(field, value);

        Assert.True(result.Succeeded);
        var converted = Assert.IsType<GeneralLinkTypedFieldValue>(result.Value!.Value);
        Assert.Equal(GeneralLinkKind.External, converted.Value.Kind);
        Assert.Equal("https://templarcms.dev/help", converted.Value.Url?.ToString());
        Assert.Equal("Help", converted.Value.Text);
    }

    [Fact]
    public void Convert_ReturnsStructuredInternalValue_ForGeneralLink()
    {
        var itemId = Guid.NewGuid();
        var field = CreateField(FieldType.GeneralLink, "help-link");
        var value = CreateValue(
            $"{{\"kind\":\"internal\",\"itemId\":\"{itemId}\",\"text\":\"Contact us\"}}",
            "help-link");

        var result = _converter.Convert(field, value);

        Assert.True(result.Succeeded);
        var converted = Assert.IsType<GeneralLinkTypedFieldValue>(result.Value!.Value);
        Assert.Equal(GeneralLinkKind.Internal, converted.Value.Kind);
        Assert.Equal(new ContentItemId(itemId), converted.Value.ItemId);
        Assert.Equal("Contact us", converted.Value.Text);
    }

    [Fact]
    public void Convert_ReturnsStructuredLegacyUrlValue_ForGeneralLink()
    {
        var field = CreateField(FieldType.GeneralLink, "help-link");
        var value = CreateValue("https://templarcms.dev/help", "help-link");

        var result = _converter.Convert(field, value);

        Assert.True(result.Succeeded);
        var converted = Assert.IsType<GeneralLinkTypedFieldValue>(result.Value!.Value);
        Assert.Equal(GeneralLinkKind.External, converted.Value.Kind);
        Assert.Equal("https://templarcms.dev/help", converted.Value.Url?.ToString());
    }

    [Fact]
    public void Convert_ReturnsIntegerValue_ForIntegerField()
    {
        var field = CreateField(FieldType.Integer, "sort-order");
        var value = CreateValue("42", "sort-order");

        var result = _converter.Convert(field, value);

        Assert.True(result.Succeeded);
        var converted = Assert.IsType<IntegerTypedFieldValue>(result.Value!.Value);
        Assert.Equal(42, converted.Value);
    }

    [Fact]
    public void Convert_ReturnsDecimalValue_ForDecimalField()
    {
        var field = CreateField(FieldType.Decimal, "price");
        var value = CreateValue("12.34", "price");

        var result = _converter.Convert(field, value);

        Assert.True(result.Succeeded);
        var converted = Assert.IsType<DecimalTypedFieldValue>(result.Value!.Value);
        Assert.Equal(12.34m, converted.Value);
    }

    [Fact]
    public void Convert_ReturnsDateTimeValue_ForDateTimeField()
    {
        var field = CreateField(FieldType.DateTime, "publish-on");
        var value = CreateValue("2026-06-30T13:45:00Z", "publish-on");

        var result = _converter.Convert(field, value);

        Assert.True(result.Succeeded);
        var converted = Assert.IsType<DateTimeTypedFieldValue>(result.Value!.Value);
        Assert.Equal(
            new DateTime(2026, 6, 30, 13, 45, 0, DateTimeKind.Utc),
            converted.Value);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("False", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void Convert_ReturnsBooleanValue_ForCheckboxField(
        string rawValue,
        bool expected)
    {
        var field = CreateField(FieldType.Checkbox, "is-visible");
        var value = CreateValue(rawValue, "is-visible");

        var result = _converter.Convert(field, value);

        Assert.True(result.Succeeded);
        var converted = Assert.IsType<BooleanTypedFieldValue>(result.Value!.Value);
        Assert.Equal(expected, converted.Value);
    }

    [Fact]
    public void Convert_ReturnsNullValue_WhenStoredValueIsNull()
    {
        var field = CreateField(FieldType.Integer, "sort-order");
        var value = CreateValue(null, "sort-order");

        var result = _converter.Convert(field, value);

        Assert.True(result.Succeeded);
        Assert.IsType<NullTypedFieldValue>(result.Value!.Value);
    }

    [Fact]
    public void Convert_ReturnsNullValue_WhenStoredFieldValueIsMissing()
    {
        var field = CreateField(FieldType.SingleLineText, "title");

        var result = _converter.Convert(field, null);

        Assert.True(result.Succeeded);
        Assert.IsType<NullTypedFieldValue>(result.Value!.Value);
        Assert.Null(result.Value.Source);
    }

    [Fact]
    public void Convert_ReturnsValidationError_WhenIntegerValueIsInvalid()
    {
        var field = CreateField(FieldType.Integer, "sort-order");
        var value = CreateValue("forty-two", "sort-order");

        var result = _converter.Convert(field, value);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("InvalidIntegerFieldValue", error.Code);
        Assert.Equal("sort-order", error.Target);
    }

    [Fact]
    public void Convert_ReturnsValidationError_WhenCheckboxValueIsInvalid()
    {
        var field = CreateField(FieldType.Checkbox, "is-visible");
        var value = CreateValue("yes", "is-visible");

        var result = _converter.Convert(field, value);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("InvalidCheckboxFieldValue", error.Code);
        Assert.Equal("is-visible", error.Target);
    }

    [Fact]
    public void Convert_ReturnsValidationError_WhenDecimalValueIsInvalid()
    {
        var field = CreateField(FieldType.Decimal, "price");
        var value = CreateValue("twelve", "price");

        var result = _converter.Convert(field, value);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("InvalidDecimalFieldValue", error.Code);
        Assert.Equal("price", error.Target);
    }

    [Fact]
    public void Convert_ReturnsValidationError_WhenDateTimeValueIsInvalid()
    {
        var field = CreateField(FieldType.DateTime, "publish-on");
        var value = CreateValue("tomorrow afternoon", "publish-on");

        var result = _converter.Convert(field, value);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("InvalidDateTimeFieldValue", error.Code);
        Assert.Equal("publish-on", error.Target);
    }

    [Fact]
    public void Convert_ReturnsValidationError_WhenGeneralLinkValueIsInvalid()
    {
        var field = CreateField(FieldType.GeneralLink, "help-link");
        var value = CreateValue(
            "{\"kind\":\"external\",\"url\":\"not-a-url\"}",
            "help-link");

        var result = _converter.Convert(field, value);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("InvalidGeneralLinkFieldValue", error.Code);
        Assert.Equal("help-link", error.Target);
    }

    [Fact]
    public void Convert_ReturnsValidationError_WhenFieldTypeIsUnsupported()
    {
        var field = CreateField(FieldType.Json, "metadata");
        var value = CreateValue("{\"featured\":true}", "metadata");

        var result = _converter.Convert(field, value);

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("UnsupportedFieldValueConversion", error.Code);
        Assert.Equal("metadata", error.Target);
    }

    private static FieldDefinition CreateField(FieldType fieldType, string key)
    {
        return new FieldDefinition(
            new FieldId(Guid.NewGuid()),
            key,
            key,
            fieldType);
    }

    private static ContentFieldValue CreateValue(string? value, string fieldKey)
    {
        return new ContentFieldValue(
            new ContentItemId(Guid.NewGuid()),
            new FieldId(Guid.NewGuid()),
            fieldKey,
            new ContentLanguage("en"),
            ContentVersion.First,
            value);
    }
}
