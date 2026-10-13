namespace TemplarCMS.Persistence.Security;

/// <summary>Durable installation marker; absence never triggers automatic reseeding.</summary>
public sealed class AdminProvisioningRow
{
    public int Id { get; set; } = 1;
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
