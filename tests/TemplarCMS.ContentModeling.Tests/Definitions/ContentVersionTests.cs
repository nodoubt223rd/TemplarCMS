using TemplarCMS.ContentModeling.Definitions;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Definitions;

public sealed class ContentVersionTests
{
    [Fact]
    public void Constructor_Throws_WhenVersionIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ContentVersion(-1));
    }

    [Fact]
    public void Shared_ReturnsVersionZero()
    {
        Assert.Equal(0, ContentVersion.Shared.Value);
    }

    [Fact]
    public void First_ReturnsVersionOne()
    {
        Assert.Equal(1, ContentVersion.First.Value);
    }

    [Fact]
    public void UsesValueEquality()
    {
        Assert.Equal(
            new ContentVersion(1),
            new ContentVersion(1));
    }
}
