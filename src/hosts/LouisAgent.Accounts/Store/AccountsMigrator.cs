using LouisAgent.Accounts.Messages;
using LouisAgent.Accounts.Users;
using LouisAgent.Composition.Logging;
using Microsoft.EntityFrameworkCore;

namespace LouisAgent.Accounts.Store;

/// <summary>
/// Brings the accounts database up to date when the host starts: the migrations it hasn't had yet, and the default
/// tenant's row, which every account on a host no row names points at.
/// </summary>
public static class AccountsMigrator
{
    /// <summary>Applies what's missing and makes sure the default tenant exists; says what it did in the log.</summary>
    public static async Task MigrateAsync(AccountsDbContext db, string defaultTenant, ILogger log,
        CancellationToken cancellationToken = default)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        await db.Database.MigrateAsync(cancellationToken);

        // The default tenant is a row like any other, so the accounts' foreign key holds for it too.
        if (await db.Tenants.FindAsync([defaultTenant], cancellationToken) is null)
        {
            db.Tenants.Add(new Tenant
            {
                Id = defaultTenant,
                Name = defaultTenant
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        log.Log(LogLevel.Information, AccountLogMessage.Migrated,
            new
            {
                Applied = pending.Count,
                Tenant = defaultTenant
            }, tag: "ACCOUNTS");
    }
}
