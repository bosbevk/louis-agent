namespace LouisAgent.Accounts.Tenants;

/// <summary>
/// The host name a request came to, in the one form a tenant's row names it: lower case, no port, no leading
/// <c>www.</c>, so <c>WWW.Acme.example:8080</c> and <c>acme.example</c> are the same tenant.
/// </summary>
public static class TenantHost
{
    /// <summary>The prefix a site's www address carries, which never picks a different tenant.</summary>
    private const string Www = "www.";

    /// <summary>The host as a tenant's row names it; empty stays empty.</summary>
    public static string Normalize(string? host)
    {
        string name = (host ?? "").Trim().ToLowerInvariant();

        // The port is the same site. A colon inside an IPv6 address's brackets isn't a port; one after them is.
        int colon = name.LastIndexOf(':');
        if (colon > name.LastIndexOf(']')) name = name[..colon];

        return name.StartsWith(Www, StringComparison.Ordinal) ? name[Www.Length..] : name;
    }
}
