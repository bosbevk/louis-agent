using LouisAgent.Web.Components.Site.Text;
using LouisAgent.Web.Components.Site.Turns;

namespace LouisAgent.Web.Components.Site.Guide;

/// <summary>
/// Shows every control a site can configure, live and in its states, each with a line on what it can do. A site
/// places it on a page of its own; switching the look on that page changes every piece in it.
/// </summary>
/// <remarks>
/// The style and component guide (Louis, 2026-11-16: "add a guide like that showcasing each component and its
/// abilities", "under framework on the website", "it is not private"). It lives in the framework beside the controls
/// it shows, so a new control is added here as it is built (ADR 0125).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class ThemeGuide
{
    /// <summary>The playing turn's example lines.</summary>
    private static readonly IReadOnlyList<TurnLine> TurnExample =
    [
        new(TurnLineKind.Prompt, "› are you open on Sunday?", 0),
        new(TurnLineKind.Think, "thinking · check the hours first", 1.2),
        new(TurnLineKind.Tool, "ReadHours", 2.4, "sunday"),
        new(TurnLineKind.Answer, "Yes, from nine to one. Shall I book you a table?", 3.8)
    ];

    /// <summary>The steps' example.</summary>
    private static readonly IReadOnlyList<Step> StepsExample =
    [
        new("A short call.", "We pin down the one job your agent does first."),
        new("See it work.", "A working agent on made-up data."),
        new("It's yours.", "Hosted and kept running.")
    ];

    /// <summary>How many times the guide's buttons were pressed, so a press shows it worked.</summary>
    private int _presses;

    /// <summary>What the pill under the buttons says.</summary>
    /// <remarks>A switch expression on the count, one arm a case, since there are three (ADR 0051).</remarks>
    private string PressedText => _presses switch
    {
        0 => "Not pressed yet",
        1 => "Pressed once",
        _ => $"Pressed {_presses} times"
    };

    /// <summary>A press of any of the guide's buttons counts.</summary>
    private void Press() => _presses++;

    /// <summary>The term list's example entries.</summary>
    private static readonly IReadOnlyList<Term> Terms =
    [
        new(["Hours"], "When you are open, day by day."),
        new(["Services", "Bookings"], "What you offer, and how people book it.")
    ];
}
