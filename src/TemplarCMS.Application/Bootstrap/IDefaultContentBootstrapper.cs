namespace TemplarCMS.Application.Bootstrap;

/// <summary>
/// Ensures the default CMS templates and content tree exist.
/// </summary>
public interface IDefaultContentBootstrapper
{
    /// <summary>
    /// Creates the default templates and content structure when missing.
    /// </summary>
    Task EnsureInitializedAsync(
        CancellationToken cancellationToken = default);
}
