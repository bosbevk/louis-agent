using LouisAgent.Accounts.Store;
using Microsoft.EntityFrameworkCore;

namespace LouisAgent.Accounts.Tenants;

/// <summary>
/// Picks the tenant whose row names the request's host, and the instance's default tenant for any other host, so
/// <c>localhost</c> and the public site land in the same place until a company's host is a row.
/// </summary>
/// <remarks>
/// The by-host Strategy behind <see cref="ITenantResolver"/>. One query per request; a cache can come when there are
/// enough tenants to matter.
/// </remarks>
public sealed class HostTenantResolver(AccountsDbContext db, string defaultTenant) : ITenantResolver
{
    /// <summary>The row that owns the host, else the default.</summary>
    public async Task<string> ResolveAsync(string? host, CancellationToken cancellationToken = default)
    {
        string name = TenantHost.Normalize(host);
        if (name.Length == 0) return defaultTenant;

        string? owner = await db.Tenants.Where(t => t.Host == name).Select(t => t.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return owner ?? defaultTenant;
    }
}
