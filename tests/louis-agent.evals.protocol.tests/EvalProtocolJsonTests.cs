using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Run;

namespace LouisAgent.Evals.Protocol.Tests;

/// <summary>
/// The protocol's lines read into their records and write back the same, and anything that isn't one of our events is
/// a person's line.
/// </summary>
public class EvalProtocolJsonTests
{
    /// <summary>The golden file: one hand-written line per event, from today's fields.</summary>
    private static string[] GoldenLines() =>
        File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "eval-protocol.jsonl"))
            .Where(line => line.Length > 0)
            .ToArray();

    /// <summary>Every golden line reads into its record and writes back byte for byte.</summary>
    [Test]
    public void TheGoldenLines_ReadIntoTheirRecords_AndWriteBackTheSame()
    {
        string[] lines = GoldenLines();

        var written = lines.Select(line => EvalProtocolJson.Read(line) is { } e ? EvalProtocolJson.Line(e) : null);

        Assert.That(written, Is.EqualTo(lines));
    }

    /// <summary>The golden file holds every kind of event once, so a new event can't go without a line there.</summary>
    [Test]
    public void TheGoldenFile_HasOneLinePerEvent()
    {
        var kinds = GoldenLines().Select(line => EvalProtocolJson.Read(line)?.GetType()).ToList();

        Assert.That(kinds, Is.EquivalentTo(EventSamples.All().Select(e => e.GetType())));
    }

    /// <summary>Each event, nulls and empty lists included, comes back as the same record with the same line.</summary>
    [TestCaseSource(typeof(EventSamples), nameof(EventSamples.All))]
    public void AnEvent_RoundTrips(EvalEvent sample)
    {
        string line = EvalProtocolJson.Line(sample);

        var read = EvalProtocolJson.Read(line);

        Assert.That(read?.GetType(), Is.EqualTo(sample.GetType()));
        Assert.That(EvalProtocolJson.Line(read!), Is.EqualTo(line));
    }

    /// <summary>
    /// The event's name is the first member and the time the second, as a person reading the log expects.
    /// </summary>
    [TestCaseSource(typeof(EventSamples), nameof(EventSamples.All))]
    public void ALine_StartsWithItsEvent_ThenItsTime(EvalEvent sample)
    {
        string line = EvalProtocolJson.Line(sample);

        Assert.That(line, Does.Match("""^\{"event":"[a-z-]+","t":"2026-10-28T09:30:15\.25\+00:00","""));
    }

    /// <summary>A line is one line: a newline inside a text is escaped, never written.</summary>
    [Test]
    public void ALine_HoldsNoNewline()
    {
        var lines = EventSamples.All().Select(EvalProtocolJson.Line);

        Assert.That(lines, Has.None.Contain('\n'));
    }

    /// <summary>A name we don't know, or a line for people, is not an event.</summary>
    [TestCase("""{"event":"lunch","t":"2026-10-28T08:00:00+00:00"}""")]
    [TestCase("""{"t":"2026-10-28T08:00:00+00:00","mode":"m","repeat":1}""")]
    [TestCase("Running sample-eval, 2 repeats")]
    [TestCase("""@{"event":"mode_done","t":1761458726.768,"mode":"m","repeat":1}""")]
    [TestCase("""[{"event":"mode-done"}]""")]
    [TestCase("""{"event":"mode-done","t":""")]
    [TestCase("")]
    public void ALineThatIsNoEvent_ReadsAsNothing(string line)
    {
        Assert.That(EvalProtocolJson.Read(line), Is.Null);
    }

    /// <summary>A known event with a field missing, or null where it can't be, is not an event either.</summary>
    [TestCase("""{"event":"mode-done","t":"2026-10-28T08:00:00+00:00","repeat":1,"rates":null}""")]
    [TestCase("""{"event":"mode-done","t":"2026-10-28T08:00:00+00:00","mode":null,"repeat":1,"rates":null}""")]
    [TestCase("""{"event":"mode-done","t":"2026-10-28T08:00:00+00:00","mode":"m","repeat":1}""")]
    public void AnEventWithAFieldMissing_ReadsAsNothing(string line)
    {
        Assert.That(EvalProtocolJson.Read(line), Is.Null);
    }

    /// <summary>A word that isn't one of a kind's wire names, or a kind given as a number, is not an event.</summary>
    [TestCase("\"FixedRule\"")]
    [TestCase("0")]
    public void AnEventWithAKindNotOnTheWire_ReadsAsNothing(string kind)
    {
        // A real summary line with one kind count, its kind swapped for the one under test.
        var summary = new SummaryEvent(DateTimeOffset.UnixEpoch, "m", "overall", 0, 0, 0, 0, 0, [], true, null, null,
            null, [], [new KindCount(GraderKind.FixedRule, 0, 0, 0)]);
        string wire = $"\"kind\":\"{GraderKind.FixedRule.Wire()}\"";
        string line = EvalProtocolJson.Line(summary).Replace(wire, $"\"kind\":{kind}");

        Assert.That(line, Does.Not.Contain(wire));
        Assert.That(EvalProtocolJson.Read(line), Is.Null);
    }

    /// <summary>A line written by something else, with the event's name not first, still reads.</summary>
    [Test]
    public void AnEventNameThatIsNotFirst_StillReads()
    {
        const string line =
            """{"t":"2026-10-28T08:00:00+00:00","mode":"m","event":"mode-done","repeat":1,"rates":null}""";

        Assert.That(EvalProtocolJson.Read(line), Is.TypeOf<ModeDoneEvent>());
    }
}
