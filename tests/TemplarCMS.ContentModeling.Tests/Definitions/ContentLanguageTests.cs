using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Definitions;

public sealed class ContentLanguageTests
{
    [Fact]
    public void Constructor_NormalizesLanguage()
    {
        var language = new ContentLanguage(" EN-US ");

        Assert.Equal("en-us", language.Name);
    }

    [Fact]
    public void Constructor_Throws_WhenLanguageIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new ContentLanguage(string.Empty));
    }

    [Fact]
    public void ToString_ReturnsNormalizedValue()
    {
        var language = new ContentLanguage(" EN-US ");

        Assert.Equal("en-us", language.ToString());
    }
}
