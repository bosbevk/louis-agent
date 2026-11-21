using LouisAgent.Accounts.Users;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LouisAgent.Accounts.Store;

/// <summary>
/// The accounts database: Identity's tables for users, their logins, claims and tokens, plus our tenants. An email is
/// unique within a tenant, not across the whole table, so two tenants can each hold ann@example.com.
/// </summary>
/// <remarks>
/// Identity's own context (<see cref="IdentityDbContext{TUser}"/>) with its model changed in one place: the indexes
/// it makes unique on the user name and the email alone are taken out and made again with the tenant in front. That
/// keeps Identity's managers and its migrations machinery as they are; the alternative, our own tables and a user
/// store from scratch, would be the whole of Identity again for one column.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed class AccountsDbContext(DbContextOptions<AccountsDbContext> options)
    : IdentityDbContext<AccountUser>(options)
{
    /// <summary>The tenants: the public site, and each company on a business site.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Identity's tables, then the tenants, then the per-tenant uniqueness.</summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tenant>(tenant =>
        {
            tenant.ToTable("Tenants");
            tenant.Property(t => t.Id).HasMaxLength(64);
            tenant.Property(t => t.Name).HasMaxLength(256);
            tenant.Property(t => t.Host).HasMaxLength(256);
            tenant.HasIndex(t => t.Host).IsUnique();
        });

        builder.Entity<AccountUser>(user =>
        {
            user.Property(u => u.TenantId).HasMaxLength(64);
            user.Property(u => u.DisplayName).HasMaxLength(256);
            user.HasOne<Tenant>().WithMany().HasForeignKey(u => u.TenantId);

            // Identity makes the user name unique on its own and indexes the email alone; within a tenant is the rule
            // here, so both indexes are made again with the tenant in front, under Identity's names.
            RemoveIndex(user, nameof(AccountUser.NormalizedUserName));
            RemoveIndex(user, nameof(AccountUser.NormalizedEmail));
            user.HasIndex(u => new
            {
                u.TenantId,
                u.NormalizedUserName
            }).HasDatabaseName("UserNameIndex").IsUnique();
            user.HasIndex(u => new
            {
                u.TenantId,
                u.NormalizedEmail
            }).HasDatabaseName("EmailIndex").IsUnique();
        });
    }

    /// <summary>Takes out the index Identity made on this one column.</summary>
    private static void RemoveIndex(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<AccountUser> user,
        string property)
    {
        var column = user.Metadata.FindProperty(property)!;
        var index = user.Metadata.FindIndex(column);
        if (index is not null) user.Metadata.RemoveIndex(index);
    }
}
