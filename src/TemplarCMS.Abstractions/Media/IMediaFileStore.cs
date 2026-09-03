namespace TemplarCMS.Abstractions.Media;

public interface IMediaFileStore
{
    Task SaveAsync(string storedFileName, Stream source, CancellationToken cancellationToken = default);
    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default);
}
