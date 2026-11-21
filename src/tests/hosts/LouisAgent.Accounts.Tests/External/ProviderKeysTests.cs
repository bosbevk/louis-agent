using LouisAgent.Accounts.Config;
using LouisAgent.Accounts.External;
using LouisAgent.Choices;

namespace LouisAgent.Accounts.Tests.External;

/// <summary>A provider is on only with both keys, read under one prefix; its wire name is the one name it goes by.</summary>
public class ProviderKeysTests
{
    [TestCase("id", "secret", true)]
    [TestCase("id", null, false)]
    [TestCase(null, "secret", false)]
    [TestCase("  ", "secret", false)]
    [TestCase(null, null, false)]
    public void BothKeys_OrNothing(string? id, string? secret, bool expected) =>
        Assert.That(new ProviderKeys(id, secret).IsSet, Is.EqualTo(expected));

    [Test]
    public void TheKeys_AreReadUnderOnePrefixEach()
    {
        var values = new Dictionary<string, string>
        {
            ["AUTH_GOOGLE_CLIENT_ID"] = " g-id ",
            ["AUTH_GOOGLE_CLIENT_SECRET"] = "g-secret",
            ["AUTH_LINKEDIN_CLIENT_ID"] = "l-id"
        };

        var options = AccountsOptions.FromEnvironment(name => values.GetValueOrDefault(name));

        Assert.Multiple(() =>
        {
            Assert.That(options.Google, Is.EqualTo(new ProviderKeys("g-id", "g-secret")));
            Assert.That(options.Google!.IsSet, Is.True);
            Assert.That(options.Microsoft!.IsSet, Is.False);
            Assert.That(options.LinkedIn!.IsSet, Is.False, "a secret is missing");
        });
    }

    [TestCase(ExternalProvider.Google, "google")]
    [TestCase(ExternalProvider.Microsoft, "microsoft")]
    [TestCase(ExternalProvider.LinkedIn, "linkedin")]
    [TestCase(ExternalProvider.Facebook, "facebook")]
    [TestCase(ExternalProvider.X, "x")]
    public void AProvidersWireName_IsItsSchemeAndItsRoute(ExternalProvider provider, string wire) =>
        Assert.That(provider.Wire(), Is.EqualTo(wire));
}
