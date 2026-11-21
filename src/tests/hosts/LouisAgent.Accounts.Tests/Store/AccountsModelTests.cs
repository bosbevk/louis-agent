using LouisAgent.Accounts.Store;
using LouisAgent.Accounts.Users;
using Microsoft.EntityFrameworkCore;

namespace LouisAgent.Accounts.Tests.Store;

/// <summary>
/// The model the context builds, read without a server: an email and a user name are unique within a tenant, and
/// Identity's indexes on them alone are gone.
/// </summary>
public class AccountsModelTests
{
    /// <summary>The model, built on the SQL Server provider with an address nothing answers at; nothing connects.</summary>
    private static Microsoft.EntityFrameworkCore.Metadata.IEntityType Users()
    {
        using var context = new AccountsDbContext(AccountsDbContextFactory.Options("Server=nowhere;Encrypt=true"));
        return context.Model.FindEntityType(typeof(AccountUser))!;
    }

    /// <summary>Both of Identity's indexes now start with the tenant, and both are unique.</summary>
    [TestCase("UserNameIndex", nameof(AccountUser.NormalizedUserName))]
    [TestCase("EmailIndex", nameof(AccountUser.NormalizedEmail))]
    public void AnIndexIdentityMade_IsNowPerTenant_AndUnique(string name, string column)
    {
        var index = Users().GetIndexes().Single(i => i.GetDatabaseName() == name);

        Assert.Multiple(() =>
        {
            Assert.That(index.Properties.Select(p => p.Name),
                Is.EqualTo(new[] { nameof(AccountUser.TenantId), column }));
            Assert.That(index.IsUnique, Is.True);
        });
    }

    /// <summary>No index is left on the user name or the email alone, so nothing enforces uniqueness across tenants.</summary>
    [Test]
    public void NoIndex_IsOnTheUserNameOrTheEmailAlone()
    {
        var alone = Users().GetIndexes().Where(i => i.Properties.Count == 1)
            .Select(i => i.Properties[0].Name).ToList();

        Assert.That(alone, Does.Not.Contain(nameof(AccountUser.NormalizedUserName))
            .And.Not.Contain(nameof(AccountUser.NormalizedEmail)));
    }

    /// <summary>The model is Identity's schema version 3, which has the passkey table (F61.5).</summary>
    [Test]
    public void TheModel_HasThePasskeyTable()
    {
        using var context = new AccountsDbContext(AccountsDbContextFactory.Options("Server=nowhere;Encrypt=true"));

        Assert.That(context.Model.FindEntityType(typeof(Microsoft.AspNetCore.Identity.IdentityUserPasskey<string>)),
            Is.Not.Null);
    }

    /// <summary>A user points at a tenant row, so an account can't name a tenant that doesn't exist.</summary>
    [Test]
    public void AUser_BelongsToATenantRow()
    {
        var key = Users().GetForeignKeys().Single(k => k.PrincipalEntityType.ClrType == typeof(Tenant));

        Assert.That(key.Properties.Select(p => p.Name), Is.EqualTo(new[] { nameof(AccountUser.TenantId) }));
    }
}
