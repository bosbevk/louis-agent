using Microsoft.AspNetCore.Components;

namespace LouisAgent.Api.Site.Rendering;

/// <summary>
/// The address a page is rendered at, for a page that asks where it is (F121). Nothing navigates on the server, so a
/// request to go elsewhere is ignored; the app in the browser does that once it starts.
/// </summary>
internal sealed class StaticNavigation : NavigationManager
{
    /// <summary>Stands at the address, its site's root as the base.</summary>
    public StaticNavigation(Uri address) =>
        Initialize(new Uri(address, "/").ToString(), address.ToString());

    /// <inheritdoc />
    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
    }
}
