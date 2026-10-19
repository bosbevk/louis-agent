using System.Security.Cryptography;
using System.Text;

namespace LouisAgent.Api.Admin;

/// <summary>
/// The gate on <c>/admin</c> (F27): ADMIN_API_KEY, falling back to AGENT_API_KEY; with neither the portal is closed.
/// Pure, so it is tested without a web host; the endpoints read the header and call <see cref="Check"/>.
/// </summary>
public static class AdminGate
{
    /// <summary>The key the portal needs: the admin key, else the agent key, else null (closed).</summary>
    public static string? Key(string? adminKey, string? agentKey) =>
        !string.IsNullOrWhiteSpace(adminKey) ? adminKey.Trim() : !string.IsNullOrWhiteSpace(agentKey) ? agentKey.Trim() : null;

    /// <summary>Compares in fixed time, as the API's own key check does, so a wrong key reveals nothing by timing.</summary>
    public static AdminAccess Check(string? configured, string presented)
    {
        if (configured is null) return AdminAccess.Closed;
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented ?? ""), Encoding.UTF8.GetBytes(configured))
            ? AdminAccess.Allowed
            : AdminAccess.Unauthorized;
    }

    /// <summary>The key a request presents: <c>x-api-key</c>, or <c>Authorization: Bearer</c>; empty when neither.</summary>
    public static string Presented(HttpRequest request) =>
        request.Headers["x-api-key"].FirstOrDefault()
        ?? request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
        ?? string.Empty;
}
