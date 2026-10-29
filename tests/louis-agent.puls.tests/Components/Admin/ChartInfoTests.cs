using System.Text;
using System.Text.RegularExpressions;
using LouisAgent.Puls.Components.Admin;
using LouisAgent.Web.Testing;

namespace LouisAgent.Puls.Tests.Components.Admin;

/// <summary>
/// Pins what a chart's title and its hover note show, for every measure and for one the note doesn't know, so moving
/// the notes out of the component keeps every word (golden: <c>ChartInfo.html</c>).
/// </summary>
public partial class ChartInfoTests
{
    /// <summary>Every measure the notes know by its word, and one they don't.</summary>
    private static readonly string[] Topics =
    [
        "cost", "tokens", "time", "cache", "tools", "sources", "passed", "passat", "passhat", "value",
        "nothing-known"
    ];

    /// <summary>Each measure's title with its note, and an unknown one's title alone.</summary>
    [Test]
    public void EveryMeasuresNote_IsShownAsBefore()
    {
        using var ctx = new BunitContext();
        var markup = new StringBuilder();

        foreach (string topic in Topics)
        {
            var cut = ctx.Render<ChartInfo>(p => p
                .Add(x => x.Topic, topic)
                .Add(x => x.Title, $"Title of {topic}"));
            markup.AppendLine(TipId().Replace(cut.Markup, "tip-ID"));
        }

        Snapshots.Match("louis-agent.puls.tests", "Components/Admin/ChartInfo.html", markup.ToString());
    }

    /// <summary>A note's id, fresh on every render.</summary>
    [GeneratedRegex("tip-[0-9a-f]{8}")]
    private static partial Regex TipId();
}
