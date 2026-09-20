using System.Text.Json;

namespace TemplarCMS.ContentModeling.Organization;

public sealed class JsonTemplateOrganizationRepository(string templatesPath) : ITemplateOrganizationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public string DirectoryPath { get; } = Path.Combine(Path.GetFullPath(templatesPath), "Organization");
    private string DataPath => Path.Combine(DirectoryPath, "tree.json");

    public async Task<FileStream> AcquireAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(DirectoryPath);
        var started = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { return new FileStream(Path.Combine(DirectoryPath, "write.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (started.Elapsed < TimeSpan.FromSeconds(10))
            { await Task.Delay(50, cancellationToken); }
        }
    }

    public async Task<TemplateOrganizationSnapshot> ReadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(DataPath)) return TemplateOrganizationSnapshot.Empty;
        await using var stream = new FileStream(DataPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        var data = await JsonSerializer.DeserializeAsync<TemplateOrganizationSnapshot>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException("Template organization is empty or invalid.");
        Validate(data);
        return data;
    }

    public async Task SaveAsync(TemplateOrganizationSnapshot snapshot, Guid expectedRevision, CancellationToken cancellationToken = default)
    {
        await using var gate = await AcquireAsync(cancellationToken);
        await SaveLockedAsync(snapshot, expectedRevision, cancellationToken);
    }

    // Callers coordinating definition files must hold AcquireAsync throughout the operation.
    public async Task SaveLockedAsync(TemplateOrganizationSnapshot snapshot, Guid expectedRevision, CancellationToken cancellationToken)
    {
        Validate(snapshot);
        if ((await ReadAsync(cancellationToken)).Revision != expectedRevision)
            throw new TemplateOrganizationConflictException("Template organization changed. Refresh and try again.");
        await WriteAtomicAsync(DataPath, JsonSerializer.Serialize(snapshot with { Revision = Guid.NewGuid() }, JsonOptions), cancellationToken);
    }

    public static async Task WriteAtomicAsync(string path, string text, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(text);
                await stream.WriteAsync(bytes, cancellationToken);
                stream.Flush(true);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static void Validate(TemplateOrganizationSnapshot data)
    {
        var result = TemplateOrganizationValidator.Validate(data);
        if (!result.IsValid) throw new InvalidDataException(string.Join(" ", result.Errors.Select(e => e.Message)));
    }
}
