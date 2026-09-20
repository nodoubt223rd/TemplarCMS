using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;

namespace TemplarCMS.Domain.Content;

public enum ContentOrderDirection { Up, Down, First, Last }
public sealed record ContentOrderEntry(Guid Id, string Key, Guid FieldId, string? RawValue, bool Supported)
{
    public int? SortOrder => ContentSiblingOrdering.Parse(RawValue);
}
public sealed record ContentOrderSnapshot(Guid? ParentId, string Revision, IReadOnlyList<ContentOrderEntry> Items);
public sealed class ContentOrderConflictException(string message) : InvalidOperationException(message);

public static class ContentSiblingOrdering
{
    public static int? Parse(string? value) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    public static IReadOnlyList<ContentOrderEntry> Sort(IEnumerable<ContentOrderEntry> entries) => entries
        .OrderBy(e => e.SortOrder.HasValue ? 0 : 1).ThenBy(e => e.SortOrder)
        .ThenBy(e => e.Key, StringComparer.OrdinalIgnoreCase).ThenBy(e => e.Id).ToArray();
    public static string Revision(Guid? parent, IEnumerable<ContentOrderEntry> entries) => Convert.ToHexString(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(new { parent, items = entries.OrderBy(e => e.Id).ToArray() })));
    public static IReadOnlyList<ContentOrderEntry> Move(IEnumerable<ContentOrderEntry> entries, Guid id, ContentOrderDirection direction)
    {
        var ordered = Sort(entries).ToList();
        if (ordered.Any(e => !e.Supported)) throw new ContentOrderConflictException("All siblings need a shared text __sortorder field before this branch can be ordered.");
        var index = ordered.FindIndex(e => e.Id == id);
        if (index < 0) throw new ContentOrderConflictException("The item moved or was deleted. Refresh the branch.");
        var destination = direction switch
        {
            ContentOrderDirection.Up => Math.Max(0, index - 1),
            ContentOrderDirection.Down => Math.Min(ordered.Count - 1, index + 1),
            ContentOrderDirection.First => 0,
            ContentOrderDirection.Last => ordered.Count - 1,
            _ => throw new ArgumentException("Invalid ordering direction.")
        };
        var selected = ordered[index];
        ordered.RemoveAt(index);
        ordered.Insert(destination, selected);
        return ordered;
    }
}
