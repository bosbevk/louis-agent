namespace LouisAgent.Accounts.Users;

/// <summary>
/// Who the accounts belong to: the public site is one tenant, and a company on a business site is another. A tenant
/// may own a host name, so a request to that host lands in it.
/// </summary>
/// <remarks>
/// A class (ADR 0040): EF Core maps it, and a tenant's name and host change over its life. Tenancy as data, not
/// deployment: one database, a tenant column on every account, and a second tenant is a row.
/// </remarks>
public sealed class Tenant
{
    /// <summary>A short id in lower case, such as <c>puls</c>; what every account's <c>TenantId</c> points at.</summary>
    public string Id { get; set; } = "";

    /// <summary>The name people see.</summary>
    public string Name { get; set; } = "";

    /// <summary>The host name that belongs to this tenant, lower case, no port; null when no host names it.</summary>
    public string? Host { get; set; }
}
