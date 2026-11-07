using LouisAgent.Choices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace LouisAgent.Accounts.External;

/// <summary>
/// Sign in with LinkedIn, which is OpenID Connect ("Sign In with LinkedIn using OpenID Connect"): Microsoft's generic
/// OpenID Connect handler reads LinkedIn's discovery document, so no package of LinkedIn's or anyone else's is needed.
/// </summary>
/// <remarks>
/// The authority is the issuer LinkedIn's discovery document names; the code flow with the answer in the query, since
/// LinkedIn posts no form back; the three scopes it supports; and the name and email from its userinfo endpoint.
/// </remarks>
public sealed class LinkedInProvider(ProviderKeys keys) : IExternalProvider
{
    /// <summary>The issuer, where <c>.well-known/openid-configuration</c> is found.</summary>
    public const string Authority = "https://www.linkedin.com/oauth";

    public ExternalProvider Name => ExternalProvider.LinkedIn;

    public string Label => "LinkedIn";

    public bool IsConfigured => keys.IsSet;

    /// <summary>LinkedIn's OpenID Connect flow, back at <c>/signin-linkedin</c>, into Identity's external cookie.</summary>
    public void AddTo(AuthenticationBuilder auth) => auth.AddOpenIdConnect(Name.Wire(), Label, linkedin =>
    {
        linkedin.Authority = Authority;
        linkedin.ClientId = keys.ClientId!;
        linkedin.ClientSecret = keys.ClientSecret!;
        linkedin.SignInScheme = IdentityConstants.ExternalScheme;
        linkedin.CallbackPath = "/signin-linkedin";
        linkedin.ResponseType = OpenIdConnectResponseType.Code;
        linkedin.ResponseMode = OpenIdConnectResponseMode.Query;
        linkedin.Scope.Clear();
        linkedin.Scope.Add("openid");
        linkedin.Scope.Add("profile");
        linkedin.Scope.Add("email");
        linkedin.GetClaimsFromUserInfoEndpoint = true;
        linkedin.SaveTokens = false;
    });
}
