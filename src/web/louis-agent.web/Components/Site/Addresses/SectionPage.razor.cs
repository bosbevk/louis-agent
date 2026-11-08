using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Components.Site.Addresses;

/// <summary>
/// The router's last resort for a place's address (F118, ADR 0107): an address no page's route matches, such as
/// <c>/pricing/plans</c>, draws the page it names, so every section, card and page of a paged section is an address
/// someone can open, share or be sent to, without any page declaring more than its own route.
/// </summary>
/// <remarks>
/// A catch-all route, which Blazor tries only after every other, and a <see cref="DynamicComponent"/> for the page it
/// finds: the page is drawn as it would be at its own address, inside the same layout.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0107-every-section-has-an-address.md">ADR 0107: every section has an address</seealso>
public partial class SectionPage
{
    /// <summary>The address, without its leading slash.</summary>
    [Parameter] public string? Path { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = default!;

    [Inject] private NavigationManager Nav { get; set; } = default!;

    /// <summary>The page the address names, when it names one.</summary>
    private Type? Drawn { get; set; }

    /// <summary>Finds the page the address names, or hands it to the not-found page.</summary>
    protected override void OnParametersSet()
    {
        var addresses = Services.GetService<SiteAddresses>();
        var read = addresses?.Read("/" + Path);
        Drawn = read is null ? null : addresses!.PageType(read.Page);
        if (Drawn is null) Nav.NotFound();
    }
}
