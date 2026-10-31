using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace TemplarCMS.Api.Security;

/// <summary>Immutable startup snapshot. Key rotation or revocation requires a restart.</summary>
public sealed class DeliveryKeyValidator
{
    private readonly (string Name, byte[] Digest)[] _keys;
    public DeliveryKeyValidator(IOptions<DeliveryKeyOptions> options)
    {
        var value = options.Value;
        var validation = new DeliveryKeyOptionsValidator().Validate(null, value);
        if (validation.Failed) throw new OptionsValidationException(Options.DefaultName, typeof(DeliveryKeyOptions), validation.Failures);
        _keys = value.KeyDigests.Select(entry => (entry.Key, Convert.FromHexString(entry.Value))).ToArray();
    }

    public bool TryAuthenticate(string rawKey, out string? keyName)
    {
        keyName = null;
        if (rawKey is null || rawKey.Length != 43 || rawKey.Any(character =>
            !(character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_')) ||
            // A canonical 32-byte base64url value has two zero padding bits.
            "AEIMQUYcgkosw048".IndexOf(rawKey[^1], StringComparison.Ordinal) < 0) return false;
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        foreach (var key in _keys)
            if (CryptographicOperations.FixedTimeEquals(digest, key.Digest)) keyName = key.Name;
        return keyName is not null;
    }
}
