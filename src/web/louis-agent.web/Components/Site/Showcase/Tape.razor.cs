using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showcase;

/// <summary>
/// Runs a line of short items across the page without end, each with a part in the accent, fading out at both ends:
/// what the agents did, at a glance.
/// </summary>
/// <remarks>
/// The design page's tape (FX163, Louis: "also want this scrolling feature animation as a framework control"), its
/// rules taken as they stand and drawn from the theme's names; a site configures it with records (ADR 0125).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class Tape
{
    /// <summary>The items, in the order they run.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<TapeItem> Items { get; set; } = [];
}
