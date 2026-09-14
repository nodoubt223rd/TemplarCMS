using Microsoft.EntityFrameworkCore;
using TemplarCMS.Persistence.Content;
using TemplarCMS.Persistence.Media;
using TemplarCMS.Domain.Content;

namespace TemplarCMS.Persistence;

/// <summary>
/// EF Core database context for TemplarCMS persistence.
/// </summary>
public sealed class TemplarCmsDbContext : DbContext
{
    public TemplarCmsDbContext(
        DbContextOptions<TemplarCmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<PersistenceContentItem> ContentItems => Set<PersistenceContentItem>();

    public DbSet<PersistenceContentFieldValue> ContentFieldValues => Set<PersistenceContentFieldValue>();
    public DbSet<PersistenceMediaAsset> MediaAssets => Set<PersistenceMediaAsset>();
    public DbSet<Security.DirectoryUserRow> DirectoryUsers => Set<Security.DirectoryUserRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<Security.DirectoryUserRow>();
        user.ToTable("DirectoryUsers");
        user.HasKey(u => u.Id);
        user.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        user.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        user.Property(u => u.Email).HasMaxLength(254).IsRequired();
        user.Property(u => u.NormalizedEmail).HasMaxLength(254).IsRequired();
        user.HasIndex(u => u.NormalizedEmail).IsUnique();
        user.Property(u => u.Language).HasMaxLength(35).IsRequired();
        user.Property(u => u.Status).HasMaxLength(20).IsRequired();
        user.Property(u => u.RolesJson).IsRequired();
        user.Property(u => u.Revision).IsConcurrencyToken();
        var item =
            modelBuilder.Entity<PersistenceContentItem>();

        item.ToTable("ContentItems");
        item.HasKey(value => value.Id);
        item.Property(value => value.Name).HasMaxLength(AuthoringLimits.Name).IsRequired();
        item.Property(value => value.Icon).HasMaxLength(AuthoringLimits.Icon);
        item.Property(value => value.Key)
            .HasMaxLength(450)
            .IsRequired();
        item.HasIndex(value => value.TemplateId);
        item.HasIndex(value => new { value.ParentId, value.Key }).IsUnique();
        if (Database.IsSqlServer())
        {
            item.HasIndex(value => new { value.ParentId, value.Key }).HasFilter("[ParentId] IS NOT NULL");
            item.HasIndex(value => value.Key).IsUnique().HasDatabaseName("IX_ContentItems_Root_Key").HasFilter("[ParentId] IS NULL");
        }

        item.HasMany(value => value.FieldValues)
            .WithOne(value => value.Item)
            .HasForeignKey(value => value.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        var fieldValue =
            modelBuilder.Entity<PersistenceContentFieldValue>();

        fieldValue.ToTable("ContentFieldValues");
        fieldValue.HasKey(value => value.Id);
        fieldValue.Property(value => value.FieldKey).HasMaxLength(AuthoringLimits.FieldKey).IsRequired();
        fieldValue.HasIndex(value => value.FieldKey);
        fieldValue.Property(value => value.Language)
            .HasMaxLength(450)
            .IsRequired();
        fieldValue.HasIndex(
            value => new
            {
                value.ItemId,
                value.FieldId,
                value.Language,
                value.Version
            })
            .IsUnique();

        var mediaAsset = modelBuilder.Entity<PersistenceMediaAsset>();
        mediaAsset.ToTable("MediaAssets");
        mediaAsset.HasKey(value => value.Id);
        mediaAsset.Property(value => value.FileName).HasMaxLength(AuthoringLimits.FileName).IsRequired();
        mediaAsset.Property(value => value.StoredFileName).HasMaxLength(AuthoringLimits.StoredFileName).IsRequired();
        mediaAsset.Property(value => value.ContentType).HasMaxLength(AuthoringLimits.ContentType).IsRequired();
        mediaAsset.Property(value => value.AltText).HasMaxLength(AuthoringLimits.AltText);
        mediaAsset.Property(value => value.Title).HasMaxLength(AuthoringLimits.Title);
        mediaAsset.HasIndex(value => value.FolderId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepareChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepareChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepareChanges()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            foreach (var property in entry.Properties)
                if (property.Metadata.GetMaxLength() is { } maximum && property.CurrentValue is string value)
                    AuthoringLimits.Check(value, maximum, property.Metadata.Name);
            if (entry.Entity is PersistenceContentItem item)
            {
                var now = DateTimeOffset.UtcNow;
                if (entry.State == EntityState.Added) item.CreatedUtc = now;
                else entry.Property(nameof(PersistenceContentItem.CreatedUtc)).IsModified = false;
                item.ModifiedUtc = now;
            }
        }
    }
}
