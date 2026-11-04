using System.Globalization;

namespace LouisAgent.Puls.Admin.Trends;

/// <summary>
/// Where a trend panel puts its runs and its values, and the line it draws through them: the runs spread evenly across
/// the width in time, a value up from the axis to the panel's top, and a line broken wherever a run has no value.
/// </summary>
/// <remarks>
/// A plain class beside the component (Separate by concern: logic that needs no component gets its own tests). The line
/// breaks at a gap rather than crossing it (the unattended run's finding 5, as d3's <c>line.defined()</c>): a value
/// nobody measured is drawn as no line, never as a segment that claims one.
/// </remarks>
public sealed class TrendGeometry(IReadOnlyList<TrendRun> runs, double top)
{
    /// <summary>The panel's drawing size, and the space left at its edges.</summary>
    public const double Width = 600, Height = 150, Pad = 8;

    /// <summary>Where a run sits across the panel; a lone run sits in the middle.</summary>
    public double X(string run)
    {
        if (runs.Count == 1) return Width / 2;
        int index = runs.ToList().FindIndex(r => r.Run == run);
        return Pad + index * (Width - 2 * Pad) / (runs.Count - 1);
    }

    /// <summary>How high a value sits: nought on the axis, the panel's top at the top.</summary>
    public double Y(double value) => Height - Pad - value / top * (Height - 2 * Pad);

    /// <summary>
    /// The line through a mode's values, in run order: one stroke per stretch of runs that all have a value, and
    /// nothing for a run on its own between gaps (its dot is drawn apart).
    /// </summary>
    public string Path(IEnumerable<(string Run, double? Value)> values)
    {
        var known = values.Where(v => v.Value is not null).ToDictionary(v => v.Run, v => v.Value!.Value);
        var strokes = new List<string>();
        var stretch = new List<string>();
        foreach (var run in runs)
        {
            if (known.TryGetValue(run.Run, out double value))
            {
                stretch.Add($"{(stretch.Count == 0 ? "M" : "L")}{F(X(run.Run))},{F(Y(value))}");
                continue;
            }

            // A run with no value ends the stretch: the gap stays open.
            if (stretch.Count > 1) strokes.Add(string.Join(" ", stretch));
            stretch.Clear();
        }

        if (stretch.Count > 1) strokes.Add(string.Join(" ", stretch));
        return string.Join(" ", strokes);
    }

    /// <summary>A coordinate as SVG takes it: two decimals, a point for the decimal.</summary>
    public static string F(double v) => Math.Round(v, 2).ToString(CultureInfo.InvariantCulture);
}
