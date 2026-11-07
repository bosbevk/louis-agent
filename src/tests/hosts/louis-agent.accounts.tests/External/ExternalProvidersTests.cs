using LouisAgent.Accounts.Config;
using LouisAgent.Accounts.External;
using LouisAgent.Choices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LouisAgent.Accounts.Tests.External;

/// <summary>
/// Each provider is on only with its keys, joins the pipeline under its wire name, and signs into Identity's
/// external cookie; the options it builds are read back from the services, so no provider is ever called.
/// </summary>
public class ExternalProvidersTests
{
    private static readonly ProviderKeys Keys = new("id", "secret");

    /// <summary>The options a provider registered, read back by its scheme.</summary>
    private static TOptions OptionsOf<TOptions>(IExternalProvider provider) where TOptions : AuthenticationSchemeOptions
    {
        var services = new ServiceCollection();
        services.AddLogging();
        provider.AddTo(services.AddAuthentication());
        using var built = services.BuildServiceProvider();
        return built.GetRequiredService<IOptionsMonitor<TOptions>>().Get(provider.Name.Wire());
    }

    [Test]
    public void OnlyAProviderWithKeys_IsConfigured_AndTheOrderIsTheButtons()
    {
        var options = new AccountsOptions("Server=x", "site", null, Google: Keys);

        Assert.Multiple(() =>
        {
            Assert.That(ExternalProviders.Of(options).Select(p => p.Name),
                Is.EqualTo(new[] { ExternalProvider.Google, ExternalProvider.Microsoft }));
            Assert.That(ExternalProviders.Configured(options).Select(p => p.Name),
                Is.EqualTo(new[] { ExternalProvider.Google }));
        });
    }

    [Test]
    public void Google_JoinsUnderItsName_IntoTheExternalCookie()
    {
        var google = OptionsOf<GoogleOptions>(new GoogleProvider(Keys));

        Assert.Multiple(() =>
        {
            Assert.That(google.ClientId, Is.EqualTo("id"));
            Assert.That(google.SignInScheme, Is.EqualTo(IdentityConstants.ExternalScheme));
            Assert.That(google.CallbackPath.Value, Is.EqualTo("/signin-google"));
        });
    }

    [Test]
    public void Microsoft_JoinsUnderItsName_IntoTheExternalCookie()
    {
        var microsoft = OptionsOf<MicrosoftAccountOptions>(new MicrosoftProvider(Keys));

        Assert.Multiple(() =>
        {
            Assert.That(microsoft.ClientSecret, Is.EqualTo("secret"));
            Assert.That(microsoft.SignInScheme, Is.EqualTo(IdentityConstants.ExternalScheme));
            Assert.That(microsoft.CallbackPath.Value, Is.EqualTo("/signin-microsoft"));
        });
    }
}
