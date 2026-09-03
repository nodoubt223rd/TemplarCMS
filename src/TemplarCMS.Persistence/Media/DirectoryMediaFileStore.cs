using TemplarCMS.Abstractions.Media;

namespace TemplarCMS.Persistence.Media;

public sealed class DirectoryMediaFileStore : IMediaFileStore
{
    private readonly string _rootPath;
    public DirectoryMediaFileStore(string rootPath) => _rootPath = rootPath;
    public async Task SaveAsync(string storedFileName, Stream source, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_rootPath);
        await using var destination = File.Create(Path.Combine(_rootPath, Path.GetFileName(storedFileName)));
        await source.CopyToAsync(destination, cancellationToken);
    }
    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(_rootPath, Path.GetFileName(storedFileName));
        return Task.FromResult<Stream?>(File.Exists(path) ? File.OpenRead(path) : null);
    }
}
