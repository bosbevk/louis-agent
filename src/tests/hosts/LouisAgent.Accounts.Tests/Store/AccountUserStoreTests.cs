using LouisAgent.Accounts.Store;
using LouisAgent.Accounts.Tenants;
using LouisAgent.Accounts.Users;

namespace LouisAgent.Accounts.Tests.Store;

/// <summary>Without a tenant the store refuses, before it touches any database.</summary>
public class AccountUserStoreTests
{
    /// <summary>A store on a context that connects to nothing, with the request's tenant never set.</summary>
    private static AccountUserStore Store() =>
        new(new AccountsDbContext(AccountsDbContextFactory.Options("Server=nowhere;Encrypt=true")),
            new CurrentTenant());

    [Test]
    public void FindByEmail_WithNoTenant_Refuses()
    {
        var refused = Assert.ThrowsAsync<InvalidOperationException>(() => Store().FindByEmailAsync("ANN@EXAMPLE.COM"));

        Assert.That(refused!.Message, Does.StartWith("The request's tenant isn't set"));
    }

    [Test]
    public void FindByName_WithNoTenant_Refuses() =>
        Assert.ThrowsAsync<InvalidOperationException>(() => Store().FindByNameAsync("ANN@EXAMPLE.COM"));

    /// <summary>A user that brings no tenant can't be made without one.</summary>
    [Test]
    public void Create_WithNoTenantAnywhere_Refuses()
    {
        var user = new AccountUser
        {
            UserName = "ann@example.com",
            Email = "ann@example.com"
        };

        Assert.ThrowsAsync<InvalidOperationException>(() => Store().CreateAsync(user));
    }
}
