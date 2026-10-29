using LouisAgent.Choices;
using LouisAgent.Puls.Admin.Charts.Guides;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin;

/// <summary>
/// A chart's title, with the measure's hover note when the chart names a measure the notes know (the notes are
/// <c>ChartGuides</c>').
/// </summary>
public partial class ChartInfo
{
    /// <summary>A fresh id that ties the title to its note for a screen reader.</summary>
    private readonly string _id = "tip-" + Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Which measure, by its word: cost, tokens, time, cache, tools, sources, passed, passat, passhat or value.
    /// </summary>
    [Parameter, EditorRequired]
    public string Topic { get; set; } = "";

    /// <summary>The chart's title.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>
    /// The note for the measure named; none for a word the notes don't know, which shows the title alone.
    /// </summary>
    private ChartGuide? Guide =>
        SettingChoice.TryParse(Topic, out ChartTopic? topic) ? ChartGuides.For(topic!.Value) : null;
}
