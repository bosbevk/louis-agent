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
}
