using LouisAgent.Accounts.Config;
using LouisAgent.Accounts.External;
using LouisAgent.Accounts.External.Providers;
using LouisAgent.Choices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
                Is.EqualTo(new[] { ExternalProvider.Google, ExternalProvider.Microsoft, ExternalProvider.LinkedIn }));
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

    /// <summary>LinkedIn is OpenID Connect on the generic handler: its issuer, the code flow in the query, its three scopes.</summary>
    [Test]
    public void LinkedIn_IsOpenIdConnect_OnLinkedInsIssuer()
    {
        var linkedin = OptionsOf<OpenIdConnectOptions>(new LinkedInProvider(Keys));

        Assert.Multiple(() =>
        {
            Assert.That(linkedin.Authority, Is.EqualTo("https://www.linkedin.com/oauth"));
            Assert.That(linkedin.ResponseType, Is.EqualTo("code"));
            Assert.That(linkedin.ResponseMode, Is.EqualTo("query"));
            Assert.That(linkedin.Scope, Is.EqualTo(new[] { "openid", "profile", "email" }));
            Assert.That(linkedin.SignInScheme, Is.EqualTo(IdentityConstants.ExternalScheme));
            Assert.That(linkedin.CallbackPath.Value, Is.EqualTo("/signin-linkedin"));
            Assert.That(linkedin.GetClaimsFromUserInfoEndpoint, Is.True);
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
