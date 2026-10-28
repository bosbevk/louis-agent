using LouisAgent.Testing;
using System.Collections;
using System.Globalization;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Trials;
using LouisAgent.Evals.Grading.Outcomes;

namespace LouisAgent.Evals.Tests.Protocol;

/// <summary>
/// The lines an eval writes are the portal's protocol (F31, version 1). Each one must parse, the way the API reads it,
/// to the same record as the old Python writer's line for the same event. The fixture is the Python's own output, with
/// the clock fixed.
/// </summary>
public class ProtocolWriterTests
{
    /// <summary>The fixed moment every line is stamped with, so ours and the Python's match.</summary>
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    /// <summary>Where the Python writer's recorded lines sit beside the tests.</summary>
    private static string PythonFixture => Path.Combine(AppContext.BaseDirectory, "Fixtures", "python-protocol.txt");

    /// <summary>The very first line says which version of the protocol follows.</summary>
    [Test]
    public void TheFirstLineIsTheVersion()
    {
        var sink = new StringWriter();

        new ProtocolWriter(sink, new FixedClock(At)).Started();

        Assert.That(Describe(BenchmarkProtocol.Parse(Lines(sink)[0])), Does.StartWith("ProtocolStarted{Version=1"));
    }

    /// <summary>Accents and dashes in a question are escaped, so every byte written is plain ASCII.</summary>
    [Test]
    public void EveryLine_IsAscii()
    {
        var sink = new StringWriter();

        new ProtocolWriter(sink, new FixedClock(At)).Question("cook", 1, "pannekoek", 1, "sess_1",
            "Gee my 'n resep – rand, Ünïcode");

        Assert.That(sink.ToString().All(c => c < 128), Is.True, sink.ToString());
    }

    /// <summary>A whole run's worth of events reads back line for line the same as the Python's.</summary>
    [Test]
    public void EachEvent_ParsesToTheRecordThePythonLineParsesTo()
    {
        // Write the same events, in the same order, as the Python fixture holds.
        var sink = new StringWriter();
        var writer = new ProtocolWriter(sink, new FixedClock(At));
        writer.Started();
        writer.Mode("cook", 1, ["web", "shops"], "puls-cook", null);
        writer.Question("cook", 1, "pannekoek", 1, "sess_1", "Gee my 'n resep vir pannekoek – rand");
        writer.Sse("cook", 1, "pannekoek", 1, "content_block_delta",
            Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo – wêreld"}}"""));
        writer.Turn("cook", 1, "pannekoek", 1, "sess_1", "Gee my 'n resep vir pannekoek – rand", "Hallo – wêreld",
            "puls-cook", 1.5);
        writer.Conversation("cook", 1, "pannekoek", "puls-cook", Json("""{"routed_right":true,"priced":2}"""), 3.25);
        writer.ModeDone("cook", 1);

        string[] mine = Lines(sink);
        string[] python = File.ReadAllLines(PythonFixture).Where(l => l.Length > 0).ToArray();

        // Compare what each pair of lines means, not how it is spaced.
        Assert.That(mine, Has.Length.EqualTo(python.Length));
        for (int i = 0; i < python.Length; i++)
            Assert.That(Describe(BenchmarkProtocol.Parse(mine[i])),
                Is.EqualTo(Describe(BenchmarkProtocol.Parse(python[i]))),
                $"line {i + 1}: {mine[i]}");
    }

    /// <summary>The first line names the eval, its version, the commit, its graders and the models it uses.</summary>
    [Test]
    public void TheFirstLine_NamesTheEvalItRuns()
    {
        var sink = new StringWriter();

        new ProtocolWriter(sink, new FixedClock(At)).Begin("cook", 2, "abc123",
            [new FakeGrader("priced-with-source", 1, GraderKind.FixedRule)],
            [new ModelUse("cook", ModelLevel.Agent, "puls-cook", "claude-haiku-5-5")]);

        var started = (BenchmarkProtocol.ProtocolStarted)BenchmarkProtocol.Parse(Lines(sink)[0]);
        Assert.That((started.Version, started.Eval, started.EvalVersion, started.Commit),
            Is.EqualTo((1, "cook", (int?)2, "abc123")));
        Assert.That(started.Graders!.Single(),
            Is.EqualTo(new BenchmarkProtocol.GraderInfo("priced-with-source", 1, BenchmarkGraderKind.FixedRule)));
        Assert.That(started.Models!.Single(),
            Is.EqualTo(new BenchmarkProtocol.ModelInfo("cook", BenchmarkModelLevel.Agent, "puls-cook",
                "claude-haiku-5-5")));
    }

    /// <summary>
    /// The first line says which scenarios the run asked for. Without it a held-out run would be stored like a dev
    /// one, and would join the dev trend.
    /// </summary>
    [TestCase(SplitSelection.Dev, BenchmarkRunSplit.Dev, "dev")]
    [TestCase(SplitSelection.HeldOut, BenchmarkRunSplit.HeldOut, "held_out")]
    [TestCase(SplitSelection.All, BenchmarkRunSplit.All, "all")]
    public void TheFirstLine_SaysWhichScenariosTheRunAskedFor(SplitSelection split, BenchmarkRunSplit expected,
        string wire)
    {
        var sink = new StringWriter();

        new ProtocolWriter(sink, new FixedClock(At)).Begin("cook", 2, "abc123", [], [], split);

        Assert.That(Lines(sink)[0], Does.Contain($"\"split\":\"{wire}\""));
        Assert.That(((BenchmarkProtocol.ProtocolStarted)BenchmarkProtocol.Parse(Lines(sink)[0])).Split,
            Is.EqualTo(expected));
    }

    /// <summary>A finished conversation writes each grader's grade and how the conversation came out.</summary>
    [Test]
    public void AConversation_WritesItsGradesAndOutcome()
    {
        var sink = new StringWriter();

        new ProtocolWriter(sink, new FixedClock(At)).Conversation("cook", 1, "pannekoek", "puls-cook", Json("{}"), 3.25,
            [new Grade("priced-with-source", 1, 1, GradeOutcome.Pass, "2 of 2 priced with a source")],
            TrialOutcome.Pass);

        var ended = (BenchmarkProtocol.ConversationEnded)BenchmarkProtocol.Parse(Lines(sink)[0]);
        Assert.That(ended.Outcome, Is.EqualTo(BenchmarkOutcome.Pass));
        Assert.That(ended.Grades!.Single(),
            Is.EqualTo(new BenchmarkProtocol.GradeInfo("priced-with-source", 1, 1, BenchmarkOutcome.Pass,
                "2 of 2 priced with a source")));
    }

    /// <summary>
    /// A model grader's grade says what judged it (the judge's model and its prompt's hash); a code grader's has
    /// neither field. The API still reads the line.
    /// </summary>
    [Test]
    public void AModelGradersGrade_CarriesWhatJudged_AndACodeGradersDoesNot()
    {
        // One grade from a judge model, one from code.
        var sink = new StringWriter();
        Grade[] grades =
        [
            new Grade("meal-for-people", 3, 1, GradeOutcome.Pass, "feeds four",
                new GradeJudge("qwen3:8b", "abc123def456")),
            new Grade("priced-with-source", 1, 1, GradeOutcome.Pass, "2 of 2 priced with a source"),
        ];

        new ProtocolWriter(sink, new FixedClock(At)).Conversation("cook", 1, "pannekoek", "puls-cook", Json("{}"), 3.25,
            grades, TrialOutcome.Pass);

        // The raw line: only the judged grade carries the judge's fields.
        var written = Json(Lines(sink)[0].TrimStart('@')).GetProperty("grades").EnumerateArray().ToList();
        Assert.That(written[0].GetProperty("judge_model").GetString(), Is.EqualTo("qwen3:8b"));
        Assert.That(written[0].GetProperty("judge_prompt_hash").GetString(), Is.EqualTo("abc123def456"));
        Assert.That(written[1].TryGetProperty("judge_model", out _), Is.False);
        Assert.That(written[1].TryGetProperty("judge_prompt_hash", out _), Is.False);

        // And the API's reader still reads both grades.
        var ended = (BenchmarkProtocol.ConversationEnded)BenchmarkProtocol.Parse(Lines(sink)[0]);
        Assert.That(ended.Grades!.Select(g => g.Grader), Is.EqualTo(new[] { "meal-for-people", "priced-with-source" }));
    }

    /// <summary>A grader that is only named here: the protocol's first line says what graders the eval has.</summary>
    private sealed class FakeGrader(string name, int version, GraderKind kind) : IGrader
    {
        public string Name { get; } = name;
        public int Version { get; } = version;
        public GraderKind Kind { get; } = kind;
        public bool Required => true;
        public IReadOnlySet<string> ExpectedKeys => System.Collections.Frozen.FrozenSet<string>.Empty;

        /// <summary>Never called: these tests only write the grader's name.</summary>
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) =>
            throw new NotImplementedException();
    }

    /// <summary>
    /// A mode's line ends with how many conversations it holds, after its model, in the Python's key order, so the
    /// stub matches the Python stub.
    /// </summary>
    [Test]
    public void AModeLineCarriesItsConversations_AfterItsLlm()
    {
        var sink = new StringWriter();

        new ProtocolWriter(sink, new FixedClock(At)).Mode(SampleMode.Alpha, 1, [SampleToolset.Alpha], "puls/voice-af",
            null, conversations: 2);

        Assert.That(Keys(Lines(sink)[0]),
            Is.EqualTo(new[] { "event", "t", "mode", "repeat", "toolsets", "role", "llm", "conversations" }));
    }

    /// <summary>A turn's facts (who answered, the calls, the hosts) are written in the Python's key order.</summary>
    [Test]
    public void ATurnWithItsFacts_WritesThemInThePythonsOrder()
    {
        var sink = new StringWriter();
        var facts = new TurnFacts("puls-cook", "sub-1", 1, 3, ["www.checkers.co.za"], null);

        new ProtocolWriter(sink, new FixedClock(At)).Turn(SampleMode.Beta, 1, "bobotie", 2, "sess_1",
            "En as ek dit vir 6 wil maak?",
            "Hier is bobotie.", "puls-cook", 0.1, facts);

        Assert.That(Keys(Lines(sink)[0]),
            Is.EqualTo(new[]
            {
                "event", "t", "mode", "repeat", "task", "turn", "session", "question", "answer", "role",
                "continued", "puls_calls", "specialist_calls", "hosts", "seconds", "error",
            }));
    }

    /// <summary>A conversation's error count comes just before its seconds, as the Python writes it.</summary>
    [Test]
    public void AConversationWritesItsErrors_BeforeItsSeconds()
    {
        var sink = new StringWriter();

        new ProtocolWriter(sink, new FixedClock(At)).Conversation(SampleMode.Beta, 1, "bobotie", "puls-cook",
            Json("""{"routed_right":true,"turns":2}"""),
            3.25, errors: 1);

        Assert.That(Keys(Lines(sink)[0]),
            Is.EqualTo(new[] { "event", "t", "mode", "repeat", "task", "role", "scores", "errors", "seconds" }));
    }

    /// <summary>A line's keys in the order they were written (the leading `@` is the protocol's marker).</summary>
    private static string[] Keys(string line) =>
        JsonDocument.Parse(line.TrimStart('@')).RootElement.EnumerateObject().Select(p => p.Name).ToArray();

    /// <summary>Everything the writer wrote, one line each.</summary>
    private static string[] Lines(StringWriter sink) =>
        sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>A bit of JSON text as an element that outlives its document.</summary>
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>
    /// A record as its type and its fields, with JSON compared by its content: what the two lines mean, not how they
    /// are spaced.
    /// </summary>
    private static string Describe(BenchmarkProtocol.Event e) =>
        e.GetType().Name + "{" + string.Join(", ", e.GetType().GetProperties().Where(p => p.CanRead)
            .Select(p => $"{p.Name}={Show(p.GetValue(e))}")) + "}";

    /// <summary>One field's value as text, so two records can be compared as strings.</summary>
    private static string Show(object? value) => value switch
    {
        null => "null",
        JsonElement element => JsonSerializer.Serialize(element),
        string text => text,
        IEnumerable items => "[" + string.Join(",", items.Cast<object?>().Select(Show)) + "]",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
    };
}
