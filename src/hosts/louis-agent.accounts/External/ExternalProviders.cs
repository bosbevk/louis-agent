using LouisAgent.Accounts.Config;

namespace LouisAgent.Accounts.External;

/// <summary>The providers the library knows, each with its keys from the settings; steps 2 and 3 of F61.3 add them.</summary>
public static class ExternalProviders
{
    /// <summary>Every provider, configured or not, in the order the buttons show.</summary>
    public static IReadOnlyList<IExternalProvider> Of(AccountsOptions options) => [];

    /// <summary>The ones this instance has keys for.</summary>
    public static IReadOnlyList<IExternalProvider> Configured(AccountsOptions options) =>
        Of(options).Where(p => p.IsConfigured).ToList();
}
