using LouisAgent.Api.Contracts.Admin.Trends;
using LouisAgent.Puls.Admin.Trends;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Trends;

/// <summary>
/// A check's trend over its runs: a line per mode, a panel per measure, the reading run in the head, and a mark where a
/// run has notes or a changed prompt. The trend is read once as the contract's <c>TrendReply</c>, never picked apart by
/// name; each panel is a <c>TrendPanel</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public partial class TrendChart
{
    /// <summary>
    /// One id per chart, so its panels' clipping rectangles don't collide with another chart's on the page.
    /// </summary>
    private readonly string _id = Guid.NewGuid().ToString("N")[..8];

    /// <summary>The lines drawn, one per mode.</summary>
    private IReadOnlyList<TrendLineRow> _series = [];

    /// <summary>Every run on one axis, in time.</summary>
    private List<TrendRun> _runs = [];

    /// <summary>The runs marked for a note or a changed prompt.</summary>
    private IReadOnlyList<TrendMark> _marks = [];

    /// <summary>The lines to draw, one per mode: the contract's trend lines, or a category's in their shape.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TrendLineRow> Series { get; set; } = [];

    /// <summary>The currency the page shows money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>The card's small chart: the cost panel only.</summary>
    [Parameter]
    public bool Small { get; set; }

    /// <summary>The run the page has open, marked on every panel and read by default.</summary>
    [Parameter]
    public string? Highlight { get; set; }

    /// <summary>
    /// The panels to draw, by their measure's word (<c>passat</c>, <c>passhat</c>); every panel when none are named
    /// (F18 step 5c.4: a category's chart shows the pass rates only).
    /// </summary>
    [Parameter]
    public IReadOnlyList<string>? Panels { get; set; }

    /// <summary>A small chart inside a card (the category cards): shorter panels, the titles muted.</summary>
    [Parameter]
    public bool Compact { get; set; }

    /// <summary>Its own title; off when a folding section above already names it.</summary>
    [Parameter]
    public bool ShowTitle { get; set; } = true;

    /// <summary>
    /// A name per mode for the legend (Puls with puls-cook), when the page knows them; else the mode's name.
    /// </summary>
    [Parameter]
    public IReadOnlyDictionary<string, string>? Labels { get; set; }

    /// <summary>Which run the chart reads: the marked one, else the last.</summary>
    private TrendReading Reading => new(_runs, Highlight);

    /// <summary>The panels this chart draws, in their order.</summary>
    private IEnumerable<TrendMeasure> Shown =>
        TrendMeasures.All(Money).Where(m => Panels is null || Panels.Contains(m.Topic));

    /// <summary>
    /// Whether there's anything to draw: lines over runs, and for the card's chart a cost the ledger priced (the stub's
    /// never is, and draws nothing).
    /// </summary>
    private bool Draws =>
        _series.Count > 0 && _runs.Count > 0 &&
        (!Small || _series.SelectMany(s => s.Points).Any(p => p.Cost is not null));

    /// <summary>Reads the trend and lays every run on one axis, in time.</summary>
    protected override void OnParametersSet()
    {
        _series = Series;

        // Every run on one x axis, in time: a mode missing from a run leaves a gap, not a shifted line.
        _runs = _series.SelectMany(s => s.Points)
            .Select(p => new TrendRun(p.Run, p.At))
            .Distinct()
            .OrderBy(r => r.At)
            .ToList();
        // The runs with notes or a changed prompt, marked once each in every full panel.
        _marks = TrendMarks.Of(_series, _runs);
    }

    /// <summary>Money in the page's currency.</summary>
    private string Money(decimal usd) => UsageFormat.Money(usd, "USD", Currency);
}
