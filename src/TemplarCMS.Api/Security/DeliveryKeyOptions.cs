using Microsoft.Extensions.Options;

namespace TemplarCMS.Api.Security;

public sealed class DeliveryKeyOptions
{
    public const string SectionName = "DeliverySecurity";
    public Dictionary<string, string> KeyDigests { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class DeliveryKeyOptionsValidator : IValidateOptions<DeliveryKeyOptions>
{
    public ValidateOptionsResult Validate(string? name, DeliveryKeyOptions options)
    {
        if (options.KeyDigests is null) return ValidateOptionsResult.Fail("DeliverySecurity:KeyDigests must be a map.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var digests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in options.KeyDigests)
        {
            if (string.IsNullOrWhiteSpace(entry.Key) || !names.Add(entry.Key) || entry.Value is null ||
                entry.Value.Length != 64 || !entry.Value.All(Uri.IsHexDigit) || !digests.Add(entry.Value))
                return ValidateOptionsResult.Fail($"Invalid or duplicate delivery key configuration at DeliverySecurity:KeyDigests:{entry.Key}. Expected a unique SHA-256 hex digest.");
        }
        return ValidateOptionsResult.Success;
    }
}
