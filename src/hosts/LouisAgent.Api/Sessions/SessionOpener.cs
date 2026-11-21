using System.Security.Claims;
using LouisAgent.Accounts.Contracts;

namespace LouisAgent.Api.Sessions;

/// <summary>
/// Who opened a session, when a signed-in person did (F61.2): their id and tenant, read from the cookie's claims.
/// A session a key opened has none. The ledger row with these on it is F43's.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0088-a-tenant-on-every-account.md">ADR 0088: a tenant on every account</seealso>
public sealed record SessionOpener(string UserId, string TenantId)
{
    /// <summary>The opener the request's principal names, or null for a call with a key and no person.</summary>
    public static SessionOpener? Of(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true) return null;

        string? user = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(user)) return null;

        return new SessionOpener(user, principal.FindFirstValue(AccountClaims.Tenant) ?? "");
    }
}
