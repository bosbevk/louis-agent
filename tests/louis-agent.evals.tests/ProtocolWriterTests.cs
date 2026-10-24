using System.Collections;
using System.Globalization;
using System.Text.Json;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Evals.Protocol;

namespace LouisAgent.Evals.Tests;

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
