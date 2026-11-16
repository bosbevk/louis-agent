using LouisAgent.Web.Components.Site.Pieces;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showcase;

/// <summary>
/// Shows what an agent does as a row of cards, each with a small film that plays it, its title, its words and the
/// tool it called, under a rail a beam of light runs along.
/// </summary>
/// <remarks>
/// The design page's "What it does, not what it says" (FX162, Louis: "the design doc is the truth here"), its rules
/// taken as they stand and drawn from the theme's names; a site configures it with records (ADR 0125).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class FeatureCards
{
    /// <summary>The things it does, in order.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<Feature> Items { get; set; } = [];

    /// <summary>Whether the theme's lens bends what is behind a card under the pointer; on by default.</summary>
    [Parameter] public bool Lens { get; set; } = true;

    /// <summary>Whether a card leans towards the pointer, as far as the theme says; on by default.</summary>
    [Parameter] public bool Tilt { get; set; } = true;

    /// <summary>Whether a visitor or the guide can pick a card, the theme drawing it picked; on by default.</summary>
    [Parameter] public bool Pick { get; set; } = true;

    /// <summary>Whether each card shows its reflection under it; off by default.</summary>
    [Parameter] public bool Reflect { get; set; }

    /// <summary>A card's id from its title, for one that can be picked, so its address reads like a tile's.</summary>
    private string? AddressOf(Feature feature) => Pick ? PieceId.Of(feature.Title) : null;

    /// <summary>What the cards tell the lens: nothing while it is on, so only a switched-off lens is written.</summary>
    private string? LensSwitch => PaneSwitch.Lens(Lens);
}
