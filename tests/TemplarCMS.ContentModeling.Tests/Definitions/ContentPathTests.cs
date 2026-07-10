using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.ContentModeling.Tests.Definitions;

public sealed class ContentPathTests
{
    [Fact]
    public void Constructor_NormalizesAbsolutePath()
    {
        var path = new ContentPath(" //home//articles/hello-world// ");

        Assert.Equal("/home/articles/hello-world", path.Value);
    }

    [Fact]
    public void FromRoot_CreatesRootPathFromContentItemKey()
    {
        var path =
            ContentPath.FromRoot(
                new ContentItemKey("HOME PAGE"));

        Assert.Equal("/home-page", path.Value);
    }

    [Fact]
    public void Append_CreatesChildPathFromParentPathAndContentItemKey()
    {
        var path =
            ContentPath.Append(
                new ContentPath("/home/articles"),
                new ContentItemKey("Hello World"));

        Assert.Equal("/home/articles/hello-world", path.Value);
    }

    [Fact]
    public void Constructor_Throws_WhenPathIsEmpty()
    {
        Assert.Throws<ArgumentException>(
            () => new ContentPath(" "));
    }
}
