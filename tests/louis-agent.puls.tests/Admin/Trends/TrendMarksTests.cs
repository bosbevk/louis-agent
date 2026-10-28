using LouisAgent.Api.Contracts.Admin.Trends;
using LouisAgent.Puls.Admin.Trends;

namespace LouisAgent.Puls.Tests.Admin.Trends;

/// <summary>
/// The trend marks a run where someone wrote a note, and where an agent's prompt changed, once per run whatever the
/// number of lines it is on, with a tooltip that says what was written and which agents changed.
/// </summary>
public class TrendMarksTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 24, 9, 0, 0, TimeSpan.Zero);

    /// <summary>A point with only what the marks read: its run, its notes and the agents that changed.</summary>
    private static TrendPointRow Point(string run, int day, IReadOnlyList<string>? notes = null,
        IReadOnlyList<string>? changed = null) =>
        new(run, T0.AddDays(day), null, null, 1, null, null, null, null, null, null, null, null, null, null, notes,
            changed);

    /// <summary>The runs on the axis, in time.</summary>
    private static readonly IReadOnlyList<TrendRun> Runs =
        [new("run_1", T0), new("run_2", T0.AddDays(1)), new("run_3", T0.AddDays(2))];

    /// <summary>A note on the run shows on every line's point; the mark lists it once.</summary>
    [Test]
    public void ANoteOnTwoLines_IsOneMark_ListingItOnce()
    {
        var series = new[]
        {
            new TrendLineRow(SampleMode.Beta, [Point("run_2", 1, ["judge changed", "the cook guessed"])]),
            new TrendLineRow(SampleMode.Alpha, [Point("run_2", 1, ["judge changed"])]),
        };

        var mark = TrendMarks.Of(series, Runs).Single();

        Assert.That((mark.Run, mark.Notes), Is.EqualTo(("run_2", new[] { "judge changed", "the cook guessed" })));
        Assert.That(mark.Changes, Is.Empty);
        Assert.That(mark.NoteTip, Is.EqualTo("2 notes\n• judge changed\n• the cook guessed"));
        Assert.That(mark.ChangeTip, Is.Null);
    }

    /// <summary>A change names its line and the agents whose prompts changed on it.</summary>
    [Test]
    public void APromptChange_NamesTheLineAndItsAgents()
    {
        var series = new[]
        {
            new TrendLineRow(SampleMode.Beta, [Point("run_2", 1, changed: ["main", "sub-session:puls-cook"])]),
            new TrendLineRow(SampleMode.Alpha, [Point("run_2", 1, changed: [])]),
        };

        var mark = TrendMarks.Of(series, Runs).Single();

        Assert.That(mark.Changes, Is.EqualTo(new[] { $"{SampleMode.Beta}: main, sub-session:puls-cook" }));
        Assert.That(mark.ChangeTip,
            Is.EqualTo($"A prompt changed\n• {SampleMode.Beta}: main, sub-session:puls-cook"));
        Assert.That(mark.NoteTip, Is.Null);
    }

    /// <summary>
    /// A run with no notes and no change (compared and the same, or nothing to compare with) gets no mark, and the
    /// marks come in the axis's order.
    /// </summary>
    [Test]
    public void OnlyRunsWithSomethingToSay_AreMarked_InTimeOrder()
    {
        var series = new[]
        {
            new TrendLineRow(SampleMode.Beta,
            [
                Point("run_3", 2, ["late"]), Point("run_1", 0, [], null), Point("run_2", 1, [], ["main"]),
            ]),
        };

        var marks = TrendMarks.Of(series, Runs);

        Assert.That(marks.Select(m => m.Run), Is.EqualTo(new[] { "run_2", "run_3" }));
    }

    /// <summary>A point from an API that sends no notes yet reads as none.</summary>
    [Test]
    public void APointWithoutNotesOrChanges_IsNoMark() =>
        Assert.That(TrendMarks.Of([new TrendLineRow(SampleMode.Alpha, [Point("run_1", 0)])], Runs), Is.Empty);
}
