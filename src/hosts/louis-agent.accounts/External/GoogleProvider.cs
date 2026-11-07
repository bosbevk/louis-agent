using LouisAgent.Choices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace LouisAgent.Accounts.External;

/// <summary>Sign in with Google, on Microsoft's own handler; the person's email and name come with the sign-in.</summary>
public sealed class GoogleProvider(ProviderKeys keys) : IExternalProvider
{
    public ExternalProvider Name => ExternalProvider.Google;

    public string Label => "Google";

    public bool IsConfigured => keys.IsSet;

    /// <summary>Google's OAuth 2.0 flow, back at <c>/signin-google</c>, into Identity's external cookie.</summary>
    public void AddTo(AuthenticationBuilder auth) => auth.AddGoogle(Name.Wire(), Label, google =>
    {
        google.ClientId = keys.ClientId!;
        google.ClientSecret = keys.ClientSecret!;
        google.SignInScheme = IdentityConstants.ExternalScheme;
        google.CallbackPath = "/signin-google";
    });
}
