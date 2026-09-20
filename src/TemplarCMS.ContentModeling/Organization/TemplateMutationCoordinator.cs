using System.Text.Json;

namespace TemplarCMS.ContentModeling.Organization;

/// <summary>Serializes template mutations and rolls interrupted operations back before reuse.</summary>
public sealed class TemplateMutationCoordinator(string templatesPath, JsonTemplateOrganizationRepository organization)
{
    private readonly string root = Path.GetFullPath(templatesPath);
    private string JournalPath => Path.Combine(organization.DirectoryPath, "pending.json");
    private string TreePath => Path.Combine(organization.DirectoryPath, "tree.json");
    private sealed record Journal(Guid OperationId, Dictionary<string, string> Definitions, string? Organization);

    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        await using var gate = await organization.AcquireAsync(cancellationToken);
        await RecoverLockedAsync(cancellationToken);
        await BeginLockedAsync(cancellationToken);
        try
        {
            await operation(cancellationToken);
            File.Delete(JournalPath);
        }
        catch
        {
            // Cancellation cannot abandon rollback. A failed recovery deliberately leaves the journal.
            await RecoverLockedAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task RecoverAsync(CancellationToken cancellationToken = default)
    {
        await using var gate = await organization.AcquireAsync(cancellationToken);
        await RecoverLockedAsync(cancellationToken);
    }

    // These methods require the organization write lock, also used by endpoint coordination.
    public async Task BeginLockedAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(JournalPath)) throw new InvalidOperationException("Recover the pending template operation first.");
        var definitions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.GetFiles(root, "*.json", SearchOption.TopDirectoryOnly))
            definitions.Add(Path.GetFileName(path), await File.ReadAllTextAsync(path, cancellationToken));
        var journal = new Journal(Guid.NewGuid(), definitions,
            File.Exists(TreePath) ? await File.ReadAllTextAsync(TreePath, cancellationToken) : null);
        await JsonTemplateOrganizationRepository.WriteAtomicAsync(JournalPath, JsonSerializer.Serialize(journal), cancellationToken);
    }

    public async Task RecoverLockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(JournalPath)) return;
        var journal = JsonSerializer.Deserialize<Journal>(await File.ReadAllTextAsync(JournalPath, cancellationToken))
            ?? throw new InvalidDataException("Invalid pending template operation.");
        if (journal.OperationId == Guid.Empty || journal.Definitions is null ||
            journal.Definitions.Any(entry => entry.Value is null || string.IsNullOrEmpty(entry.Key) ||
                Path.GetFileName(entry.Key) != entry.Key || entry.Key.IndexOfAny(['/', '\\', ':']) >= 0 ||
                !entry.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Invalid definition paths in pending template operation.");
        foreach (var (name, text) in journal.Definitions)
            await JsonTemplateOrganizationRepository.WriteAtomicAsync(Path.Combine(root, name), text, cancellationToken);
        var names = journal.Definitions.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var path in Directory.GetFiles(root, "*.json", SearchOption.TopDirectoryOnly))
            if (!names.Contains(Path.GetFileName(path))) File.Delete(path);
        if (journal.Organization is { } tree)
            await JsonTemplateOrganizationRepository.WriteAtomicAsync(TreePath, tree, cancellationToken);
        else if (File.Exists(TreePath)) File.Delete(TreePath);
        File.Delete(JournalPath);
    }
}
