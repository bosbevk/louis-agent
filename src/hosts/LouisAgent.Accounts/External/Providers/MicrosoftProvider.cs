using LouisAgent.Choices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace LouisAgent.Accounts.External.Providers;

/// <summary>
/// Sign in with a Microsoft account, personal or work, on Microsoft's own handler (an Entra app registration that
/// allows both kinds of account).
/// </summary>
public sealed class MicrosoftProvider(ProviderKeys keys) : IExternalProvider
{
    public ExternalProvider Name => ExternalProvider.Microsoft;

    public string Label => "Microsoft";

    public bool IsConfigured => keys.IsSet;

    /// <summary>Microsoft's OAuth 2.0 flow, back at <c>/signin-microsoft</c>, into Identity's external cookie.</summary>
    public void AddTo(AuthenticationBuilder auth) => auth.AddMicrosoftAccount(Name.Wire(), Label, microsoft =>
    {
        microsoft.ClientId = keys.ClientId!;
        microsoft.ClientSecret = keys.ClientSecret!;
        microsoft.SignInScheme = IdentityConstants.ExternalScheme;
        microsoft.CallbackPath = "/signin-microsoft";
    });
}
