using System.Security.Claims;
using LouisAgent.Accounts.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LouisAgent.Accounts.Users;

/// <summary>
/// What the sign-in cookie says about a person: Identity's claims (the id, the name, the email) plus the tenant, so
/// a host reads who opened a session and where from the cookie alone, without a lookup.
/// </summary>
/// <remarks>
/// Identity's own extension point for the principal (Template Method): the base makes the standard claims, this adds
/// one. A claim is signed with the cookie, so a client can't change it, where a tenant the browser sent could be
/// anything.
/// </remarks>
public sealed class AccountClaimsFactory(UserManager<AccountUser> users, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<AccountUser>(users, options)
{
    /// <summary>Identity's claims, then the tenant.</summary>
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AccountUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(AccountClaims.Tenant, user.TenantId));
        return identity;
    }
}
