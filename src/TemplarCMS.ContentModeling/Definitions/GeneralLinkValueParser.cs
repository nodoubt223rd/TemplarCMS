using System.Text.Json;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.ContentModeling.Definitions;

/// <summary>
/// Parses persisted general link string values into structured runtime values.
/// </summary>
public static class GeneralLinkValueParser
{
    public static GeneralLinkValue Parse(string rawValue, string fieldKey)
    {
        ArgumentNullException.ThrowIfNull(rawValue);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldKey);

        var trimmedValue = rawValue.Trim();

        if (Guid.TryParse(trimmedValue, out var itemId))
        {
            return GeneralLinkValue.Internal(
                new ContentItemId(itemId));
        }

        if (TryParseAbsoluteUri(trimmedValue, out var uri))
        {
            return GeneralLinkValue.External(uri);
        }

        try
        {
            using var document =
                JsonDocument.Parse(trimmedValue);
            var root =
                document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw InvalidValue(
                    fieldKey,
                    rawValue,
                    "must be a JSON object.");
            }

            var kind =
                GetRequiredString(root, "kind", fieldKey, rawValue)
                    .Trim()
                    .ToLowerInvariant();
            var text =
                GetOptionalString(root, "text");
            var target =
                GetOptionalString(root, "target");

            return kind switch
            {
                "internal" => ParseInternal(root, fieldKey, rawValue, text, target),
                "external" => ParseExternal(root, fieldKey, rawValue, text, target),
                _ => throw InvalidValue(
                    fieldKey,
                    rawValue,
                    "must declare kind 'internal' or 'external'.")
            };
        }
        catch (JsonException)
        {
            throw InvalidValue(
                fieldKey,
                rawValue,
                "must be either an absolute URL, a content item id, or a JSON general link object.");
        }
    }

    private static GeneralLinkValue ParseInternal(
        JsonElement root,
        string fieldKey,
        string rawValue,
        string? text,
        string? target)
    {
        var itemIdValue =
            GetRequiredString(root, "itemId", fieldKey, rawValue);

        if (!Guid.TryParse(itemIdValue, out var itemId))
        {
            throw InvalidValue(
                fieldKey,
                rawValue,
                "internal links require a valid itemId GUID.");
        }

        return GeneralLinkValue.Internal(
            new ContentItemId(itemId),
            text,
            target);
    }

    private static GeneralLinkValue ParseExternal(
        JsonElement root,
        string fieldKey,
        string rawValue,
        string? text,
        string? target)
    {
        var urlValue =
            GetRequiredString(root, "url", fieldKey, rawValue);

        if (!TryParseAbsoluteUri(urlValue, out var url))
        {
            throw InvalidValue(
                fieldKey,
                rawValue,
                "external links require an absolute URL.");
        }

        return GeneralLinkValue.External(
            url,
            text,
            target);
    }

    private static string GetRequiredString(
        JsonElement root,
        string propertyName,
        string fieldKey,
        string rawValue)
    {
        var value =
            GetOptionalString(root, propertyName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw InvalidValue(
                fieldKey,
                rawValue,
                $"must include '{propertyName}'.");
        }

        return value;
    }

    private static string? GetOptionalString(
        JsonElement root,
        string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => property.GetString(),
            _ => null
        };
    }

    private static bool TryParseAbsoluteUri(
        string value,
        out Uri uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out uri!))
        {
            return uri.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps;
        }

        uri = null!;
        return false;
    }

    private static InvalidOperationException InvalidValue(
        string fieldKey,
        string rawValue,
        string detail)
    {
        return new InvalidOperationException(
            $"Field '{fieldKey}' value '{rawValue}' {detail}");
    }
}
