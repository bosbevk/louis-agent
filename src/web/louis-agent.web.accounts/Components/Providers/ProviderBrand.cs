using LouisAgent.Choices;

namespace LouisAgent.Web.Accounts.Components.Providers;

/// <summary>
/// The sign-in providers whose own button look the pages follow, the browser's copy of the accounts host's names: it
/// can't reference the host's <c>ExternalProvider</c>, and a provider not named here keeps the plain button.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public enum ProviderBrand
{
    /// <summary>Sign in with Google.</summary>
    [WireName("google")] Google,

    /// <summary>Sign in with Microsoft.</summary>
    [WireName("microsoft")] Microsoft,

    /// <summary>Sign in with LinkedIn.</summary>
    [WireName("linkedin")] LinkedIn
}

/// <summary>What a provider's name means for its button.</summary>
public static class ProviderBrands
{
    /// <summary>The brand of a provider, by the name the API gives it; none for a provider we have no look for.</summary>
    public static ProviderBrand? Of(string name) =>
        SettingChoice.TryParse<ProviderBrand>(name, out var brand) ? brand : null;
}
