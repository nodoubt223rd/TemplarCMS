using TemplarCMS.Domain.Content;
using Xunit;

namespace TemplarCMS.Application.Tests.Content;

public sealed class ContentOrderingTests
{
    [Theory]
    [InlineData(ContentOrderDirection.Up, "b,a,c")]
    [InlineData(ContentOrderDirection.Down, "a,c,b")]
    [InlineData(ContentOrderDirection.First, "b,a,c")]
    [InlineData(ContentOrderDirection.Last, "a,c,b")]
    public void MovesSelectedSibling(ContentOrderDirection direction, string expected)
    {
        var entries = new[] { Entry("a", "0"), Entry("b", "1"), Entry("c", "2") };
        var ordered = ContentSiblingOrdering.Move(entries, entries[1].Id, direction);
        Assert.Equal(expected, string.Join(",", ordered.Select(e => e.Key)));
    }

    [Fact]
    public void InvalidAndTiedValuesUseDeterministicKeysAndRevisionIncludesRawValues()
    {
        var entries = new[] { Entry("c", "bad"), Entry("b", "2"), Entry("a", "2"), Entry("d", null) };
        Assert.Equal("a,b,c,d", string.Join(",", ContentSiblingOrdering.Sort(entries).Select(e => e.Key)));
        var revision = ContentSiblingOrdering.Revision(null, entries);
        Assert.NotEqual(revision, ContentSiblingOrdering.Revision(null, [.. entries.Take(3), entries[3] with { RawValue = "bad" }]));
        Assert.Equal(revision, ContentSiblingOrdering.Revision(null, entries.Reverse()));
    }

    private static ContentOrderEntry Entry(string key, string? value) => new(Guid.NewGuid(), key, Guid.NewGuid(), value, true);
}
