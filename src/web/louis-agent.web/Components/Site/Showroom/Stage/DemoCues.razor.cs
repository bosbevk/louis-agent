using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Stage;

/// <summary>
/// What to watch in a demo (F127.1): a few short cues beside the player, each about a turn of the recording, lit as the
/// play reaches it, the newest pulsing, a bar over them filling as it goes. A visitor's eye goes to the moment that
/// matters (it asks before it books) while it happens, not in a paragraph before it.
/// </summary>
/// <remarks>
/// The page holds the turn reached, told by the recorded chat's <c>TurnStarted</c>, and hands it here; a click goes
/// back up as the cue's turn (<c>Chosen</c>), and the page plays from it: the cues know nothing of the player, so a
/// cue list goes beside any player that says its turns and plays from one.
/// </remarks>
public partial class DemoCues
{
    /// <summary>The cues, in the order they happen.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<DemoCue> Cues { get; set; } = [];

    /// <summary>The turn the play has reached, from one; none yet before it starts.</summary>
    [Parameter]
    public int Reached { get; set; }

    /// <summary>The heading over the cues, in the site's words.</summary>
    [Parameter]
    public string Heading { get; set; } = "";

    /// <summary>What a cue does when clicked, in the site's words: "Watch from here".</summary>
    [Parameter]
    public string? Hint { get; set; }

    /// <summary>Told a cue's turn when the visitor clicks it, so the page plays from there.</summary>
    [Parameter]
    public EventCallback<int> Chosen { get; set; }

    /// <summary>One cue as it's drawn: its number, its words, its classes and the turn a click plays from.</summary>
    private sealed record Row(int Number, string Text, string Classes, int Turn);

    /// <summary>The cues, numbered, each lit once its turn is reached, the reached turn's own marked as now.</summary>
    private IEnumerable<Row> Rows => Cues.Select((cue, at) => new Row(at + 1, cue.Text, ClassesOf(cue), cue.Turn));

    /// <summary>A cue's classes: waiting, lit, or lit and the one happening now.</summary>
    /// <remarks>Guard arms, one per state (ADR 0051).</remarks>
    private string ClassesOf(DemoCue cue) => cue.Turn switch
    {
        var turn when turn > Reached => "cue",
        var turn when turn == Reached => "cue is-lit is-now",
        _ => "cue is-lit",
    };

    /// <summary>How far the bar fills: the turn reached, of the last turn a cue is about.</summary>
    private string ProgressStyle
    {
        get
        {
            int last = Cues.Count == 0 ? 1 : Cues.Max(cue => cue.Turn);
            int percent = (int)Math.Round(100.0 * Math.Clamp(Reached, 0, last) / last);
            return $"width: {percent.ToString(CultureInfo.InvariantCulture)}%";
        }
    }
}
