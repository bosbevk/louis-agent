using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Components.Areas;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Pages.Admin;

/// <summary>
/// The admin's home, the same on every site (F144): a card for each area the site named (the overview, the packs, the
/// evals), in that order, each the way to its own pages.
/// </summary>
/// <remarks>
/// The home names no area: it draws whatever cards the site's areas bring (<see cref="SiteAreas.AdminCards"/>), so a
/// new area's card appears with no change here (ADR 0120). It listens to <see cref="AdminAccess"/> for a refused key,
/// so the key is asked for once, not once per card; a saved key makes every card read again.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0120-nothing-is-hand-wired-from-the-framework.md">ADR 0120: nothing is hand-wired from the framework</seealso>
public partial class AdminHome
{
    [Inject] private AdminAccess Access { get; set; } = null!;
    [Inject] private SiteAreas Areas { get; set; } = null!;

    /// <summary>The cards the site's areas bring, in the order named.</summary>
    private IReadOnlyList<Type> Cards => Areas.AdminCards;

    /// <summary>Listens for a refused key, which a card reports.</summary>
    protected override void OnInitialized() => Access.Changed += Redraw;

    /// <summary>Stops listening, since the access outlives the page.</summary>
    public void Dispose()
    {
        Access.Changed -= Redraw;
        GC.SuppressFinalize(this);
    }

    /// <summary>A card was refused, or a key was saved.</summary>
    private void Redraw() => _ = InvokeAsync(StateHasChanged);
}
