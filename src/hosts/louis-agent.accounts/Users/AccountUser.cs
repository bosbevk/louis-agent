using LouisAgent.Accounts.Messages;
using LouisAgent.Composition.Messages;
using Microsoft.AspNetCore.Identity;

namespace LouisAgent.Accounts.Users;

/// <summary>
/// A person with an account: Identity's user (id, email, password hash, lockout, confirmed or not) plus which tenant
/// they belong to, the name they go by, and when the account was made. Nobody exists outside a tenant.
/// </summary>
/// <remarks>
/// A class, not a record (ADR 0040): Identity changes it over time (a confirmed email, a new hash, a lockout), and EF
/// Core maps it. The subclass is how Identity is extended, as its docs say; the alternative, our own user type over
/// every Identity interface, would be weeks of work for the same table.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed class AccountUser : IdentityUser
{
    /// <summary>The tenant this account lives in; the unique email is unique within it.</summary>
    public string TenantId { get; set; } = "";

    /// <summary>What the person likes to be called; nothing until they say.</summary>
    public string? DisplayName { get; set; }

    /// <summary>When the account was made, in UTC (ADR 0046).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// A new account in a tenant, signing in with its email; it refuses a blank tenant, since nothing runs without one.
    /// </summary>
    public static AccountUser New(string tenantId, string email, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException(MessageText.Format(AccountErrorMessage.TenantRequired), nameof(tenantId));

        return new AccountUser
        {
            TenantId = tenantId.Trim(),
            UserName = email.Trim(),
            Email = email.Trim(),
            CreatedAt = now.ToUniversalTime()
        };
    }
}
