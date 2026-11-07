namespace LouisAgent.Accounts.Tenants;

/// <summary>
/// The tenant this request is in, set once by the middleware and read by the store and the routes. One per request
/// (scoped), so nothing has to pass it along by hand.
/// </summary>
/// <remarks>
/// A class (ADR 0040): it's a thing the request owns and the middleware fills, not a value. Empty until set; the store
/// refuses to work on an empty one, so a route mapped before the middleware fails loudly instead of landing in the
/// wrong tenant.
/// </remarks>
public sealed class CurrentTenant
{
    /// <summary>The tenant's id; empty before the middleware has run.</summary>
    public string Id { get; private set; } = "";

    /// <summary>Whether the middleware has run.</summary>
    public bool IsSet => Id.Length > 0;

    /// <summary>Fills it in, once per request.</summary>
    internal void Set(string id) => Id = id;
}
