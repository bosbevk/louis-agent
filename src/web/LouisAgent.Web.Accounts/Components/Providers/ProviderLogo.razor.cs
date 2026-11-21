using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Accounts.Components.Providers;

/// <summary>A sign-in provider's logo, as its brand guidelines give it; nothing for a provider with no brand.</summary>
/// <remarks>
/// The logos are drawn here, not loaded as files, so a site needs nothing of its own for them, and each provider's
/// rules (Google's "don't change the G", Microsoft's "don't alter the logo") hold by never touching the drawing.
/// </remarks>
/// <seealso href="https://developers.google.com/identity/branding-guidelines">Sign in with Google branding guidelines</seealso>
/// <seealso href="https://learn.microsoft.com/en-us/entra/identity-platform/howto-add-branding-in-apps">Sign in with Microsoft branding guidelines</seealso>
public partial class ProviderLogo
{
    /// <summary>Whose logo.</summary>
    [Parameter]
    public ProviderBrand? Brand { get; set; }
}
