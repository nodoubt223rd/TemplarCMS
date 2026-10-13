using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace TemplarCMS.Api.Security;

internal static class TemporaryPasswordGenerator
{
    public static string Generate() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(16));
}
