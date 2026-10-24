using System.Text;
using System.Text.Json;
using LouisAgent.SessionStream;

namespace LouisAgent.SessionStream.Tests;

/// <summary>The reader against the recorded Puls turn (its expected counts sit beside it), and against small streams.</summary>
[TestFixture]
public class SessionStreamReaderTests
{
    [Test]
    public async Task ReadsEveryEventOfTheRecordedTurn()
    {
        var events = await ReadRecordedTurnAsync();

        Assert.That(events, Has.Count.EqualTo(1503));
        Assert.That(events.OfType<UnknownEvent>(), Is.Empty);
    }

    [Test]
    public async Task PulsCallsTheExpectedToolsAtTheTop()
    {
        var events = await ReadRecordedTurnAsync();

        var names = events.Where(e => e is not SubSessionStreamed).SelectMany(ToolUseNames).ToList();

        Assert.That(names, Is.EqualTo(Expected().GetProperty("puls_tools").EnumerateArray().Select(n => n.GetString())));
    }

    [Test]
    public async Task TheCookCallsTheExpectedToolsInsideItsSubSession()
    {
        var events = await ReadRecordedTurnAsync();

        var names = events.OfType<SubSessionStreamed>().SelectMany(ToolUseNames).ToList();

        Assert.That(names, Is.EqualTo(Expected().GetProperty("specialist_tools").EnumerateArray().Select(n => n.GetString())));
    }

    [Test]
    public async Task TheRoleIsTheOneTheStartedSubSessionNamed()
    {
        var events = await ReadRecordedTurnAsync();

        var start = events.SelectMany(ToolUses).First(t => t.Name == "StartSubSession").Input;
        var role = start.TryGetProperty("role", out var r) ? r.GetString() : start.GetProperty("personality").GetString();

        Assert.That(role, Is.EqualTo(Expected().GetProperty("role").GetString()));
    }

    [Test]
    public async Task TheHostsAreTheOnesThePagesWereReadFrom()
    {
        var events = await ReadRecordedTurnAsync();

        var hosts = events.SelectMany(ToolUses).Concat(events.OfType<SubSessionStreamed>().SelectMany(ToolUses))
            .Where(t => t.Name is "FetchUrl" or "ReadRenderedPage" && t.Input.ValueKind == JsonValueKind.Object
                && t.Input.TryGetProperty("url", out _))
            .Select(t => new Uri(t.Input.GetProperty("url").GetString()!).Host.ToLowerInvariant())
            .Select(h => h.StartsWith("www.", StringComparison.Ordinal) ? h[4..] : h)
            .Distinct()
            .ToList();

        Assert.That(hosts, Is.EqualTo(Expected().GetProperty("hosts").EnumerateArray().Select(h => h.GetString())));
    }

    [Test]
    public async Task TheAnswerIsTheTextOfTheTurn()
    {
        var events = await ReadRecordedTurnAsync();

        var text = new StringBuilder();
        foreach (var e in events.OfType<ContentBlockDelta>())
        {
            if (e.Data.GetProperty("delta").GetProperty("type").GetString() == "text_delta")
            {
                text.Append(e.Data.GetProperty("delta").GetProperty("text").GetString());
            }
        }
        var answer = text.ToString().Trim();

        Assert.That(answer, Does.StartWith(Expected().GetProperty("answer_starts").GetString()!));
        Assert.That(answer.EnumerateRunes().Count(), Is.EqualTo(Expected().GetProperty("answer_length").GetInt32()));
    }

    [Test]
    public async Task TheTurnHasNoError()
    {
        var events = await ReadRecordedTurnAsync();

        Assert.That(events.OfType<ErrorReported>(), Is.Empty);
    }

    [Test]
    public async Task AnUnknownEventTypeComesThroughKeptNotDropped()
    {
        var events = await ReadAsync(
            "event: message_start\ndata: {\"type\":\"message_start\"}\n\n" +
            "event: ping_later\ndata: {\"x\":1}\n\n");

        Assert.That(events, Has.Count.EqualTo(2));
        var unknown = events[1] as UnknownEvent;
        Assert.That(unknown, Is.Not.Null);
        Assert.That(unknown!.EventType, Is.EqualTo("ping_later"));
        Assert.That(unknown.Data.GetProperty("x").GetInt32(), Is.EqualTo(1));
    }

    [Test]
    public async Task MultiLineDataIsJoinedWithNewlines()
    {
        var events = await ReadAsync("event: content_block_delta\ndata: {\"a\":\ndata: 1}\n\n");

        Assert.That(events.Single().Data.GetProperty("a").GetInt32(), Is.EqualTo(1));
    }

    [Test]
    public async Task EachEventKeepsItsRawJsonText()
    {
        const string raw = "{\"type\":\"message_start\",  \"n\": 2}";

        var events = await ReadAsync($"event: message_start\ndata: {raw}\n\n");

        Assert.That(events.Single().Data.GetRawText(), Is.EqualTo(raw));
    }

    [Test]
    public async Task ASubSessionsInnerEventIsTyped()
    {
        var events = await ReadAsync(
            "event: sub_session\ndata: {\"type\":\"sub_session\",\"tool_use_id\":\"toolu_1\",\"agent\":\"puls\"," +
            "\"event\":\"content_block_delta\",\"data\":{\"type\":\"content_block_delta\",\"index\":0," +
            "\"delta\":{\"type\":\"thinking_delta\",\"thinking\":\"hm\"}}}\n\n");

        var sub = events.Single() as SubSessionStreamed;
        Assert.That(sub, Is.Not.Null);
        Assert.That(sub!.ToolUseId, Is.EqualTo("toolu_1"));
        Assert.That(sub.Inner, Is.InstanceOf<ContentBlockDelta>());
        Assert.That(sub.Inner.Data.GetProperty("delta").GetProperty("thinking").GetString(), Is.EqualTo("hm"));
    }

    [Test]
    public async Task MessageStartedCarriesItsToolsetsAndRole()
    {
        var events = await ReadAsync(
            "event: message_start\ndata: {\"type\":\"message_start\",\"message\":{\"session_id\":\"s\"," +
            "\"toolsets\":[\"web\",\"shops\"],\"session_role\":\"puls-cook\"}}\n\n");

        var started = events.Single() as MessageStarted;
        Assert.That(started, Is.Not.Null);
        Assert.That(started!.Toolsets, Is.EqualTo(new[] { "web", "shops" }));
        Assert.That(started.Role, Is.EqualTo("puls-cook"));
    }

    [Test]
    public async Task MessageStartedWithoutToolsetsOrRoleReadsEmptyAndNull()
    {
        var events = await ReadAsync("event: message_start\ndata: {\"type\":\"message_start\",\"message\":{}}\n\n");

        var started = (MessageStarted)events.Single();
        Assert.That(started.Toolsets, Is.Empty);
        Assert.That(started.Role, Is.Null);
    }

    [Test]
    public async Task AMalformedDataLineIsKeptAsUnknownAndTheReadGoesOn()
    {
        var events = await ReadAsync(
            "event: content_block_delta\ndata: {not json\n\n" +
            "event: message_stop\ndata: {\"type\":\"message_stop\"}\n\n");

        Assert.That(events, Has.Count.EqualTo(2));
        var broken = events[0] as UnknownEvent;
        Assert.That(broken, Is.Not.Null);
        Assert.That(broken!.EventType, Is.EqualTo("content_block_delta"));
        Assert.That(broken.Data.GetString(), Is.EqualTo("{not json"));
        Assert.That(events[1], Is.InstanceOf<MessageStopped>());
    }

    [Test]
    public void FromMapsAnEventTheSameWayTheReaderDoes()
    {
        using var document = JsonDocument.Parse("{\"type\":\"message_start\",\"message\":{\"toolsets\":[\"web\"],\"session_role\":\"puls-cook\"}}");

        var started = SessionEvent.From("message_start", document.RootElement) as MessageStarted;

        Assert.That(started, Is.Not.Null);
        Assert.That(started!.Toolsets, Is.EqualTo(new[] { "web" }));
        Assert.That(started.Role, Is.EqualTo("puls-cook"));
        Assert.That(SessionEvent.From("ping", document.RootElement), Is.InstanceOf<UnknownEvent>());
    }

    private static readonly string FixtureDirectory = Path.Combine(AppContext.BaseDirectory, "Fixtures");

    private static Task<List<SessionEvent>> ReadRecordedTurnAsync() =>
        ReadAsync(File.ReadAllText(Path.Combine(FixtureDirectory, "session-stream.sse")));

    private static JsonElement Expected()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(FixtureDirectory, "session-stream.expected.json")));
        return document.RootElement.Clone();
    }

    private static async Task<List<SessionEvent>> ReadAsync(string text)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        var events = new List<SessionEvent>();
        await foreach (var e in SessionStreamReader.ReadAsync(stream, CancellationToken.None))
        {
            events.Add(e);
        }
        return events;
    }

    private sealed record ToolUse(string Name, JsonElement Input);

    /// <summary>Every tool call an event starts, including those a sub-session sent.</summary>
    private static IEnumerable<ToolUse> ToolUses(SessionEvent e) => e switch
    {
        ContentBlockStarted s when s.Data.GetProperty("content_block").GetProperty("type").GetString() == "tool_use" =>
            [new ToolUse(s.Data.GetProperty("content_block").GetProperty("name").GetString()!,
                s.Data.GetProperty("content_block").TryGetProperty("input", out var input) ? input : default)],
        SubSessionStreamed s => ToolUses(s.Inner),
        _ => [],
    };

    private static IEnumerable<string> ToolUseNames(SessionEvent e) => ToolUses(e).Select(t => t.Name);
}
