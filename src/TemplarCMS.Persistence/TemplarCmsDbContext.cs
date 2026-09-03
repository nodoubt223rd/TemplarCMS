using Microsoft.EntityFrameworkCore;
using TemplarCMS.Persistence.Content;
using TemplarCMS.Persistence.Media;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var item =
            modelBuilder.Entity<PersistenceContentItem>();

        item.ToTable("ContentItems");
        item.HasKey(value => value.Id);
        item.Property(value => value.Name).IsRequired();
        item.Property(value => value.Key).IsRequired();
        item.HasIndex(value => value.TemplateId);
        item.HasIndex(value => new { value.ParentId, value.Key }).IsUnique();

        item.HasMany(value => value.FieldValues)
            .WithOne(value => value.Item)
            .HasForeignKey(value => value.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        var fieldValue =
            modelBuilder.Entity<PersistenceContentFieldValue>();

        fieldValue.ToTable("ContentFieldValues");
        fieldValue.HasKey(value => value.Id);
        fieldValue.Property(value => value.FieldKey).IsRequired();
        fieldValue.Property(value => value.Language).IsRequired();
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
        mediaAsset.Property(value => value.FileName).IsRequired();
        mediaAsset.Property(value => value.StoredFileName).IsRequired();
        mediaAsset.Property(value => value.ContentType).IsRequired();
        mediaAsset.HasIndex(value => value.FolderId);
    }
}
