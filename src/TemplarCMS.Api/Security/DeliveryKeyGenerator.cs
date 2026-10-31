using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace TemplarCMS.Api.Security;

public sealed record GeneratedDeliveryKey(string Key, string Sha256Digest);

public static class DeliveryKeyGenerator
{
    public static GeneratedDeliveryKey Generate()
    {
        var key = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return new(key, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))));
    }
}
