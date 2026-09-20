namespace TemplarCMS.ContentModeling.Organization;

public interface ITemplateOrganizationRepository
{
    Task<TemplateOrganizationSnapshot> ReadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(TemplateOrganizationSnapshot snapshot, Guid expectedRevision, CancellationToken cancellationToken = default);
}
