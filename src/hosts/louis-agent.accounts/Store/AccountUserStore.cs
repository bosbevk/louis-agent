using LouisAgent.Accounts.Messages;
using LouisAgent.Accounts.Tenants;
using LouisAgent.Accounts.Users;
using LouisAgent.Composition.Messages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LouisAgent.Accounts.Store;

/// <summary>
/// Identity's user store, but inside the request's tenant: an email or a user name is looked up within the tenant,
/// and a new account is stamped with it, so Identity's own register and sign-in routes land in the right tenant
/// without knowing tenants exist. Without a tenant it refuses, rather than landing in the wrong one.
/// </summary>
/// <remarks>
/// A subclass of <see cref="UserStore{TUser}"/> that overrides the three calls where the tenant matters (Template
/// Method, Identity's own extension point): the managers, the endpoints and the rest of the store stay Identity's.
/// An id stays global, since ids are unique across the table; an external login too, so one Google account links in
/// one tenant only.
/// </remarks>
public sealed class AccountUserStore(AccountsDbContext db, CurrentTenant tenant, TimeProvider? time = null)
    : UserStore<AccountUser>(db)
{
    /// <summary>The request's tenant, or a refusal if the middleware never ran.</summary>
    private string Tenant => tenant.IsSet
        ? tenant.Id
        : throw new InvalidOperationException(MessageText.Format(AccountErrorMessage.TenantNotResolved));

    /// <summary>The user with this email in the request's tenant.</summary>
    public override Task<AccountUser?> FindByEmailAsync(string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        string id = Tenant;
        return Users.FirstOrDefaultAsync(u => u.TenantId == id && u.NormalizedEmail == normalizedEmail,
            cancellationToken);
    }

    /// <summary>The user with this name in the request's tenant.</summary>
    public override Task<AccountUser?> FindByNameAsync(string normalizedUserName,
        CancellationToken cancellationToken = default)
    {
        string id = Tenant;
        return Users.FirstOrDefaultAsync(u => u.TenantId == id && u.NormalizedUserName == normalizedUserName,
            cancellationToken);
    }

    /// <summary>Saves a new account, in the request's tenant and stamped with the time, unless the caller set them.</summary>
    public override Task<IdentityResult> CreateAsync(AccountUser user, CancellationToken cancellationToken = default)
    {
        if (user.TenantId.Length == 0) user.TenantId = Tenant;
        if (user.CreatedAt == default) user.CreatedAt = (time ?? TimeProvider.System).GetUtcNow();
        return base.CreateAsync(user, cancellationToken);
    }
}
