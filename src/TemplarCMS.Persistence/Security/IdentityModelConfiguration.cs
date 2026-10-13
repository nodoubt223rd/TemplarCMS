using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace TemplarCMS.Persistence.Security;

internal static class IdentityModelConfiguration
{
    public static void Configure(ModelBuilder model)
    {
        var account = model.Entity<CmsIdentityUser>();
        account.ToTable("CmsIdentityUsers");
        account.Property(user => user.Email).HasMaxLength(254);
        account.Property(user => user.NormalizedEmail).HasMaxLength(254);
        account.HasOne<DirectoryUserRow>().WithOne().HasForeignKey<CmsIdentityUser>(user => user.Id).OnDelete(DeleteBehavior.Restrict);
        model.Entity<IdentityUserClaim<Guid>>().ToTable("CmsIdentityUserClaims");
        var login = model.Entity<IdentityUserLogin<Guid>>();
        login.ToTable("CmsIdentityUserLogins");
        login.Property(value => value.LoginProvider).HasMaxLength(128);
        login.Property(value => value.ProviderKey).HasMaxLength(128);
        var token = model.Entity<IdentityUserToken<Guid>>();
        token.ToTable("CmsIdentityUserTokens");
        token.Property(value => value.LoginProvider).HasMaxLength(128);
        token.Property(value => value.Name).HasMaxLength(128);
        var marker = model.Entity<AdminProvisioningRow>();
        marker.ToTable("AdminProvisioning", table => table.HasCheckConstraint("CK_AdminProvisioning_Singleton", "Id = 1"));
        marker.HasKey(value => value.Id);
        marker.Property(value => value.Id).ValueGeneratedNever();
        marker.HasOne<CmsIdentityUser>().WithOne().HasForeignKey<AdminProvisioningRow>(value => value.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
