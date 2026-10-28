using LouisAgent.Api.Contracts.Admin.Trends;
using LouisAgent.Puls.Admin.Charts;
using LouisAgent.Puls.Admin.Trends;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Trends;

/// <summary>
/// One measure's panel of the trend: a line per mode over the runs, broken where a mode has no value, each point a link
/// to its run; hovering a point reads that run in this panel only.
/// </summary>
public partial class TrendPanel
{
    // The run a hover is reading in this panel; none when the pointer is elsewhere.
    private string? _hover;

    /// <summary>Which measure this panel draws.</summary>
    [Parameter, EditorRequired]
    public TrendMeasure Measure { get; set; } = null!;

    /// <summary>A line per mode, as the trend has them.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TrendLineRow> Series { get; set; } = [];

    /// <summary>Every run on the axis, in time.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TrendRun> Runs { get; set; } = [];

    /// <summary>Which run the chart reads by default.</summary>
    [Parameter, EditorRequired]
    public TrendReading Reading { get; set; } = null!;

    /// <summary>The run the page has open, marked across the panel.</summary>
    [Parameter]
    public string? Highlight { get; set; }

    /// <summary>The card's small chart.</summary>
    [Parameter]
    public bool Small { get; set; }

    /// <summary>A chart inside a card: no dates under the axis.</summary>
    [Parameter]
    public bool Compact { get; set; }

    /// <summary>The chart's id, so the panel's clipping rectangle is its own on the page.</summary>
    [Parameter, EditorRequired]
    public string ChartId { get; set; } = "";

    /// <summary>Whether any run has a value here: a measure nobody measured draws no empty line.</summary>
    private bool HasValues => Series.SelectMany(s => s.Points).Any(p => Measure.Value(p) is not null);

    /// <summary>The panel's top: the measure's fixed one (a share's 100%), else its highest value.</summary>
    private double Top => Measure.FixedTop ?? Math.Max(1e-9,
        Series.SelectMany(s => s.Points).Select(Measure.Value).OfType<double>().DefaultIfEmpty(0).Max());

    /// <summary>Where this panel puts its runs and values.</summary>
    private TrendGeometry Geometry => new(Runs, Top);

    /// <summary>The clipping rectangle the lines draw in from, named for this chart and measure.</summary>
    private string Clip => $"trend-clip-{ChartId}-{Measure.Topic}";

    /// <summary>The hover, as the reading wants it: this panel's measure and the run it is on.</summary>
    private (string Topic, string Run)? Hover => _hover is { } run ? (Measure.Topic, run) : null;

    /// <summary>The marked run, when it is one of this chart's.</summary>
    private string? Marked => Highlight is { } h && Runs.Any(r => r.Run == h) ? h : null;

    /// <summary>The hovered run, when it is one of this chart's.</summary>
    private string? HoveredRun => _hover is { } run && Runs.Any(r => r.Run == run) ? run : null;

    /// <summary>Each mode's value at the run the panel reads, with its change since the run before.</summary>
    private IReadOnlyList<TrendValue> Values =>
    [
        .. Series.Select((s, i) => TrendReading.Value(s.Name, ChartPalette.Series(i),
            s.Points.Select(p => new TrendSample(p.Run, p.At, Measure.Value(p))),
            Reading.ShownIn(Measure.Topic, Hover).Run, Measure.Label))
    ];

    /// <summary>A mode's points that have a value, in time.</summary>
    private IEnumerable<TrendPointRow> Known(TrendLineRow line) =>
        line.Points.Where(p => Measure.Value(p) is not null).OrderBy(p => p.At);

    /// <summary>A mode's line, broken where a run has no value.</summary>
    private string Path(TrendLineRow line) => Geometry.Path(line.Points.Select(p => (p.Run, Measure.Value(p))));

    /// <summary>The pointer is on a run's point: read that run here.</summary>
    private void Enter(string run) => _hover = run;

    /// <summary>The pointer left the panel: read the chart's run again.</summary>
    private void Leave() => _hover = null;

    /// <summary>A coordinate as SVG takes it.</summary>
    private static string F(double v) => TrendGeometry.F(v);
}
