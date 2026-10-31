using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using TemplarCMS.Api.Security;
using Xunit;

namespace TemplarCMS.Api.Tests.Security;

public sealed class DeliveryKeyTests
{
    [Fact]
    public void KeysAreRandomCanonical256BitValuesAndDigestsHashExactUtf8Text()
    {
        var first = DeliveryKeyGenerator.Generate();
        var second = DeliveryKeyGenerator.Generate();
        Assert.NotEqual(first.Key, second.Key);
        Assert.Equal(43, first.Key.Length);
        Assert.Equal(32, WebEncoders.Base64UrlDecode(first.Key).Length);
        Assert.Equal(64, first.Sha256Digest.Length);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(first.Key))), first.Sha256Digest);
        var validator = Create(new() { ["site-A"] = first.Sha256Digest.ToLowerInvariant() });
        Assert.True(validator.TryAuthenticate(first.Key, out var name));
        Assert.Equal("site-A", name);
        Assert.False(validator.TryAuthenticate(second.Key, out _));
        Assert.False(Create([]).TryAuthenticate(first.Key, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAB")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA+")]
    public void MalformedKeysFailClosed(string value)
    {
        Assert.False(Create([]).TryAuthenticate(value, out var name));
        Assert.Null(name);
        Assert.False(Create([]).TryAuthenticate(new string('A', 100_000), out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-digest")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG")]
    public void InvalidConfigurationFailsWithoutDisclosingItsValue(string value)
    {
        var failure = Assert.Throws<OptionsValidationException>(() => Create(new() { ["bad-site"] = value }));
        Assert.Contains("bad-site", failure.Message);
        if (value.Length > 0) Assert.DoesNotContain(value, failure.Message);
    }

    [Fact]
    public void DuplicateDigestsAndCaseInsensitiveNamesFailAndConfigurationIsSnapshotted()
    {
        var key = DeliveryKeyGenerator.Generate();
        Assert.Throws<OptionsValidationException>(() => Create(new() { ["a"] = key.Sha256Digest, ["b"] = key.Sha256Digest.ToLowerInvariant() }));
        Assert.Throws<OptionsValidationException>(() => Create(new() { ["a"] = key.Sha256Digest, ["A"] = DeliveryKeyGenerator.Generate().Sha256Digest }));
        var options = new Dictionary<string, string> { ["site-A"] = key.Sha256Digest };
        var validator = Create(options);
        options.Clear();
        Assert.True(validator.TryAuthenticate(key.Key, out _));
    }

    private static DeliveryKeyValidator Create(Dictionary<string, string> digests) =>
        new(Options.Create(new DeliveryKeyOptions { KeyDigests = digests }));
}
