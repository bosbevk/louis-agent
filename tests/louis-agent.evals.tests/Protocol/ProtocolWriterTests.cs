using System.Collections;
using System.Globalization;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Tests.Protocol;

// F18 step 2: the lines the eval writes are the portal's protocol (F31, version 1). Each one parses as the API reads it, to the
// same record as the Python's line for the same event: the fixture is the Python writer's own output, with the clock fixed.
public class ProtocolWriterTests
{
    private static readonly DateTimeOffset At = DateTimeOffset.FromUnixTimeMilliseconds(1700000000123);

    private static string PythonFixture => Path.Combine(AppContext.BaseDirectory, "Fixtures", "python-protocol.txt");

    [Test]
    public void TheFirstLineIsTheVersion()
    {
        var sink = new StringWriter();

        new ProtocolWriter(sink, new FixedClock(At)).Started();

        Assert.That(Describe(BenchmarkProtocol.Parse(Lines(sink)[0])), Does.StartWith("ProtocolStarted{Version=1"));
    }

    [Test]
    public void EveryLine_IsAscii()
    {
        var sink = new StringWriter();

        new ProtocolWriter(sink, new FixedClock(At)).Question("cook", 1, "pannekoek", 1, "sess_1", "Gee my 'n resep – rand, Ünïcode");

        Assert.That(sink.ToString().All(c => c < 128), Is.True, sink.ToString());
    }

    [Test]
    public void EachEvent_ParsesToTheRecordThePythonLineParsesTo()
    {
        var sink = new StringWriter();
        var writer = new ProtocolWriter(sink, new FixedClock(At));
        writer.Started();
        writer.Mode("cook", 1, ["web", "shops"], "puls-cook", null);
        writer.Question("cook", 1, "pannekoek", 1, "sess_1", "Gee my 'n resep vir pannekoek – rand");
        writer.Sse("cook", 1, "pannekoek", 1, "content_block_delta", Json("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hallo – wêreld"}}"""));
        writer.Turn("cook", 1, "pannekoek", 1, "sess_1", "Gee my 'n resep vir pannekoek – rand", "Hallo – wêreld", "puls-cook", 1.5);
        writer.Conversation("cook", 1, "pannekoek", "puls-cook", Json("""{"routed_right":true,"priced":2}"""), 3.25);
        writer.ModeDone("cook", 1);

        string[] mine = Lines(sink);
        string[] python = File.ReadAllLines(PythonFixture).Where(l => l.Length > 0).ToArray();

        Assert.That(mine, Has.Length.EqualTo(python.Length));
        for (int i = 0; i < python.Length; i++)
            Assert.That(Describe(BenchmarkProtocol.Parse(mine[i])), Is.EqualTo(Describe(BenchmarkProtocol.Parse(python[i]))),
                $"line {i + 1}: {mine[i]}");
    }

    [Test]
    public void TheFirstLine_NamesTheEvalItRuns()
    {
        var sink = new StringWriter();

        new ProtocolWriter(sink, new FixedClock(At)).Begin("cook", 2, "abc123",
            [new FakeGrader("priced-with-source", 1, GraderKind.FixedRule)], [new ModelUse("cook", ModelLevel.Agent, "puls-cook", "claude-haiku-5-5")]);

        var started = (BenchmarkProtocol.ProtocolStarted)BenchmarkProtocol.Parse(Lines(sink)[0]);
        Assert.That((started.Version, started.Eval, started.EvalVersion, started.Commit), Is.EqualTo((1, "cook", (int?)2, "abc123")));
        Assert.That(started.Graders!.Single(), Is.EqualTo(new BenchmarkProtocol.GraderInfo("priced-with-source", 1, BenchmarkGraderKind.FixedRule)));
        Assert.That(started.Models!.Single(), Is.EqualTo(new BenchmarkProtocol.ModelInfo("cook", BenchmarkModelLevel.Agent, "puls-cook", "claude-haiku-5-5")));
    }

    [Test]
    public void AConversation_WritesItsGradesAndOutcome()
    {
        var sink = new StringWriter();

        new ProtocolWriter(sink, new FixedClock(At)).Conversation("cook", 1, "pannekoek", "puls-cook", Json("{}"), 3.25,
            [new Grade("priced-with-source", 1, 1, GradeOutcome.Pass, "2 of 2 priced with a source")], TrialOutcome.Pass);

        var ended = (BenchmarkProtocol.ConversationEnded)BenchmarkProtocol.Parse(Lines(sink)[0]);
        Assert.That(ended.Outcome, Is.EqualTo(BenchmarkOutcome.Pass));
        Assert.That(ended.Grades!.Single(), Is.EqualTo(new BenchmarkProtocol.GradeInfo("priced-with-source", 1, 1, BenchmarkOutcome.Pass, "2 of 2 priced with a source")));
    }

    /// <summary>A grader that is only named here: the protocol's first line says what graders the eval has.</summary>
    private sealed class FakeGrader(string name, int version, GraderKind kind) : IGrader
    {
        public string Name { get; } = name;
        public int Version { get; } = version;
        public GraderKind Kind { get; } = kind;
        public bool Required => true;
        public ValueTask<Grade> GradeAsync(Trial trial, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private static string[] Lines(StringWriter sink) => sink.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    /// <summary>A record as its type and its fields, with JSON compared by its content: what the two lines mean, not how they are spaced.</summary>
    private static string Describe(BenchmarkProtocol.Event e) =>
        e.GetType().Name + "{" + string.Join(", ", e.GetType().GetProperties().Where(p => p.CanRead)
            .Select(p => $"{p.Name}={Show(p.GetValue(e))}")) + "}";

    private static string Show(object? value) => value switch
    {
        null => "null",
        JsonElement element => JsonSerializer.Serialize(element),
        string text => text,
        IEnumerable items => "[" + string.Join(",", items.Cast<object?>().Select(Show)) + "]",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "",
    };
}
