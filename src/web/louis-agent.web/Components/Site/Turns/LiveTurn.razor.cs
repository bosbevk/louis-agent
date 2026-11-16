using System.Globalization;
using LouisAgent.Web.Components.Site.Pieces;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Turns;

/// <summary>
/// Shows one chat turn playing in a window, over and over: a site hands it the lines as records, each with when it
/// appears, and the window fades them in, blinks a caret on the answer and draws a rule before the cost.
/// </summary>
/// <remarks>
/// Moved from the framework's site, where it was that site's own (FX162, Louis: "move all shared to core web"), and
/// configured with records rather than HTML (ADR 0125): the window is the same on any site, only the words are a
/// site's.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class LiveTurn
{
    /// <summary>What the window's bar says, such as the session and its toolsets.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>What a screen reader hears for the whole turn, since the lines only play.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = "";

    /// <summary>The turn's lines, in the order they play.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<TurnLine> Lines { get; set; } = [];

    /// <summary>The turn's cost line, in Markdown, after its rule; none for a turn without one.</summary>
    [Parameter] public string? Cost { get; set; }

    /// <summary>How many seconds into the turn the cost appears.</summary>
    [Parameter] public double CostAt { get; set; }

    /// <summary>Whether the theme's lens bends what is behind the window; on by default.</summary>
    [Parameter] public bool Lens { get; set; } = true;

    /// <summary>Whether the window leans towards the pointer, as far as the theme says; on by default.</summary>
    [Parameter] public bool Tilt { get; set; } = true;

    /// <summary>The window's glass: its colour, how much, and what it does behind it; clear water by default.</summary>
    [Parameter] public GlassLook Glass { get; set; } = GlassLook.Default;

    /// <summary>Whether the window shows its reflection under it; off by default.</summary>
    [Parameter] public bool Reflect { get; set; }

    /// <summary>What the window tells the lens: only a lens switched off is written.</summary>
    private string? LensSwitch => PaneSwitch.Lens(Lens);

    /// <summary>A line's classes for what it is.</summary>
    private static string ClassesOf(TurnLine line) => line.Kind switch
    {
        TurnLineKind.Prompt => "ln ln-prompt",
        TurnLineKind.Think => "ln ln-think",
        TurnLineKind.Guard => "ln ln-guard",
        TurnLineKind.Answer => "ln ln-answer",
        _ => "ln"
    };

    /// <summary>A delay as the line's style reads it.</summary>
    private static string DelayOf(double seconds) => seconds.ToString(CultureInfo.InvariantCulture) + "s";
}
