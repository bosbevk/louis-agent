using LouisAgent.Accounts.Config;

namespace LouisAgent.Accounts.Tests.Config;

/// <summary>The settings as a host reads them: nothing set means no accounts, a value comes back trimmed.</summary>
public class AccountsOptionsTests
{
    [Test]
    public void NothingSet_MeansNoAccounts_AndTheDefaultTenant()
    {
        var options = AccountsOptions.FromEnvironment(_ => null);

        Assert.Multiple(() =>
        {
            Assert.That(options.Enabled, Is.False);
            Assert.That(options.DefaultTenant, Is.EqualTo("site"));
            Assert.That(options.CookieDomain, Is.Null);
        });
    }

    [Test]
    public void ABlankUrl_IsNoUrl()
    {
        Assert.That(AccountsOptions.FromEnvironment(name => name == "ACCOUNTS_SQL_URL" ? "   " : null).Enabled,
            Is.False);
    }

    [Test]
    public void EverySetting_IsReadAndTrimmed()
    {
        var values = new Dictionary<string, string>
        {
            ["ACCOUNTS_SQL_URL"] = " Server=sql;Database=accounts ",
            ["ACCOUNTS_TENANT"] = " Home ",
            ["ACCOUNTS_COOKIE_DOMAIN"] = " .example.test "
        };

        var options = AccountsOptions.FromEnvironment(name => values.GetValueOrDefault(name));

        Assert.Multiple(() =>
        {
            Assert.That(options.SqlUrl, Is.EqualTo("Server=sql;Database=accounts"));
            Assert.That(options.DefaultTenant, Is.EqualTo("home"), "a tenant id is lower case, as a row names it");
            Assert.That(options.CookieDomain, Is.EqualTo(".example.test"));
            Assert.That(options.Enabled, Is.True);
        });
    }
}
