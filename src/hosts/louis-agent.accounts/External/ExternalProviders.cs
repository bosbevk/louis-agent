using LouisAgent.Accounts.Config;
using LouisAgent.Accounts.External.Providers;

namespace LouisAgent.Accounts.External;

/// <summary>The providers the library knows, each with its keys from the settings, in the order the buttons show.</summary>
public static class ExternalProviders
{
    /// <summary>Every provider, configured or not.</summary>
    public static IReadOnlyList<IExternalProvider> Of(AccountsOptions options) =>
    [
        new GoogleProvider(options.Google ?? ProviderKeys.None),
        new MicrosoftProvider(options.Microsoft ?? ProviderKeys.None),
        new LinkedInProvider(options.LinkedIn ?? ProviderKeys.None)
    ];

    /// <summary>The ones this instance has keys for.</summary>
    public static IReadOnlyList<IExternalProvider> Configured(AccountsOptions options) =>
        Of(options).Where(p => p.IsConfigured).ToList();
}
