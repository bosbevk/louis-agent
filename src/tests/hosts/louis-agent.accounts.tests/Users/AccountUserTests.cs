using LouisAgent.Accounts.Users;

namespace LouisAgent.Accounts.Tests.Users;

/// <summary>A new account is born in a tenant, signs in with its email, and knows when it was made.</summary>
public class AccountUserTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 7, 12, 0, 0, TimeSpan.FromHours(2));

    /// <summary>The tenant and the email are kept as given, trimmed, and the email is the user name too.</summary>
    [Test]
    public void New_IsInItsTenant_AndSignsInWithItsEmail()
    {
        var user = AccountUser.New(" puls ", " ann@example.com ", Now);

        Assert.Multiple(() =>
        {
            Assert.That(user.TenantId, Is.EqualTo("puls"));
            Assert.That(user.Email, Is.EqualTo("ann@example.com"));
            Assert.That(user.UserName, Is.EqualTo("ann@example.com"));
            Assert.That(user.DisplayName, Is.Null);
            Assert.That(user.EmailConfirmed, Is.False);
        });
    }

    /// <summary>The time it was made is kept in UTC, whatever zone the clock gave (ADR 0046).</summary>
    [Test]
    public void New_KeepsWhenItWasMade_InUtc()
    {
        var user = AccountUser.New("puls", "ann@example.com", Now);

        Assert.That(user.CreatedAt, Is.EqualTo(new DateTimeOffset(2026, 11, 7, 10, 0, 0, TimeSpan.Zero)));
        Assert.That(user.CreatedAt.Offset, Is.EqualTo(TimeSpan.Zero));
    }

    /// <summary>Nothing runs without a tenant: a blank one is refused, with the reason.</summary>
    [TestCase("")]
    [TestCase("   ")]
    public void New_RefusesABlankTenant(string tenant)
    {
        var refused = Assert.Throws<ArgumentException>(() => AccountUser.New(tenant, "ann@example.com", Now));

        Assert.That(refused!.Message, Does.StartWith("An account needs a tenant"));
    }
}
