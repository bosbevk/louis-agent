namespace LouisAgent.Accounts.Tenants;

/// <summary>Which tenant a request belongs to, from the host it came to.</summary>
/// <remarks>
/// Strategy (Gang of Four): by host today; a business site that picks a company from a path or a header is another
/// implementation, not a branch in the middleware. Tenancy stays data, so a second tenant is a row.
/// </remarks>
public interface ITenantResolver
{
    /// <summary>The id of the tenant that owns this host, else the instance's default; never none.</summary>
    Task<string> ResolveAsync(string? host, CancellationToken cancellationToken = default);
}
