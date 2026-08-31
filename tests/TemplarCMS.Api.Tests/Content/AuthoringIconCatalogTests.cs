using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.Api.Tests.Content;

public sealed class AuthoringIconCatalogTests
{
    [Fact]
    public void Normalize_ShouldReturnCatalogKeyInCanonicalCase()
    {
        Assert.Equal("article", AuthoringIconCatalog.Normalize(" Article "));
    }

    [Fact]
    public void Normalize_ShouldRejectUnknownIcon()
    {
        Assert.Throws<ArgumentException>(() => AuthoringIconCatalog.Normalize("custom-svg"));
    }
}
