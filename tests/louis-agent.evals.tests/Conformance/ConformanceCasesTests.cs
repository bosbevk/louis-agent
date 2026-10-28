using LouisAgent.Testing;
using System.Text.Json;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Tests.Datasets;

namespace LouisAgent.Evals.Tests.Conformance;

/// <summary>
/// The conformance cases kept beside an eval (F18 step 4.2b), read from its files by scenario: the reference and each
/// known-bad beside it. A stray file (a misspelling, a deleted scenario's files, a known-bad with no reference) is
/// refused by name.
/// </summary>
public class ConformanceCasesTests
{
    /// <summary>The time every line carries.</summary>
    private static readonly DateTimeOffset At = new(2026, 10, 28, 8, 0, 0, TimeSpan.Zero);

    /// <summary>One conversation's file: its mode, one answer of "Hallo", and the turn.</summary>
    private static readonly string Conversation = string.Join("\n", new EvalEvent[]
    {
        new ModeEvent(At, SampleMode.Beta, 1, [SampleToolset.Gamma, SampleToolset.Alpha], "puls/voice-af", null, 1),
        new SseEvent(At, SampleMode.Beta, 1, "s", 1, "content_block_delta",
            Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo"}}""")),
        new TurnEvent(At, SampleMode.Beta, 1, "s", 1, "sess_1", "Hallo", "Hallo", null, null, 0, 0, [], 0.5, null,
            null),
    }.Select(EvalProtocolJson.Line));

    /// <summary>The reference gives the mode and the turns, and each known-bad is found by its label.</summary>
    [Test]
    public void LoadsTheReferenceAndEachKnownBadBeside()
    {
        using var folder = new DatasetFolder()
            .Write("s.reference.txt", Conversation)
            .Write("s.bad.slow.txt", Conversation)
            .Write("s.bad.error.txt", Conversation);

        var only = ConformanceCases.Load(folder.Root, [Scenario("s")]).Single();

        Assert.That(only.Scenario, Is.EqualTo("s"));
        Assert.That((only.Mode.Name, only.Mode.Role), Is.EqualTo((SampleMode.Beta, "puls/voice-af")));
        Assert.That(only.Mode.Toolsets, Is.EqualTo(new[] { SampleToolset.Gamma, SampleToolset.Alpha }));
        Assert.That(only.Reference.Turns.Single().Question, Is.EqualTo("Hallo"));
        Assert.That(only.KnownBad.Select(k => k.Label), Is.EqualTo(new[] { "error", "slow" }));
    }

    /// <summary>A known-bad with no reference beside it is refused, by name.</summary>
    [Test]
    public void RefusesAKnownBadWithNoReference()
    {
        using var folder = new DatasetFolder().Write("s.bad.error.txt", Conversation);

        Assert.That(() => ConformanceCases.Load(folder.Root, [Scenario("s")]),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("s.bad.error.txt"));
    }

    /// <summary>A deleted scenario's files are stray, and the loader says which.</summary>
    [Test]
    public void RefusesAFileOfAScenarioThatIsNotInTheEval()
    {
        using var folder = new DatasetFolder()
            .Write("s.reference.txt", Conversation)
            .Write("t.bad.error.txt", Conversation);

        Assert.That(() => ConformanceCases.Load(folder.Root, [Scenario("s")]),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("t.bad.error.txt"));
    }

    /// <summary>A misspelled reference is stray too.</summary>
    [Test]
    public void RefusesAMisspelledReference()
    {
        using var folder = new DatasetFolder()
            .Write("s.reference.txt", Conversation)
            .Write("s.refrence.txt", Conversation);

        Assert.That(() => ConformanceCases.Load(folder.Root, [Scenario("s")]),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("s.refrence.txt"));
    }

    /// <summary>A scenario with nothing expected of it.</summary>
    private static EvalScenario Scenario(string id) =>
        new(id, new EvalInput(null, "Etc/UTC", ["Hallo"]), Json("{}"), [], DatasetSplit.Dev);

    /// <summary>A bit of JSON text as an element that outlives its document.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();
}
