using System.Net.ServerSentEvents;
using System.Text;
using LouisAgent.Api.Sessions;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Usage.Records;
using LouisAgent.SessionStream;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests.Sessions;

// F18 step 1e, the stream's golden before the seam (CL18 step 0): one scripted turn of each kind (a model's thinking,
// text and tool call, a sub-session's event, a command reply, a cancel and an error) is written through ClaudeStyleStream
// the way the endpoint writes it (TurnStream.StreamTurnAsync), and pinned to StreamGolden.sse. The golden is the proof a
// later change to the writer (the stream format seam, F18 step 1f) moves no byte. Regenerate with LOUIS_UPDATE_GOLDEN=1.
public class StreamGoldenTests
{
    private static readonly string GoldenFile = Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.api.tests", "Sessions", "StreamGolden.sse");

    /// <summary>The one tool of the scripted turn: a note by name, as the ACP transcript's tool is.</summary>
    private sealed class NoteTools
    {
        [System.ComponentModel.Description("Reads a note by name")]
        public string ReadNote([System.ComponentModel.Description("The note's name")] string name) => $"note {name}: melktert, Sunday";
    }

    [Test]
    public async Task TheStreamMatchesItsGolden()
    {
        string text = Serialize(await ScriptedAsync());

        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1") File.WriteAllText(GoldenFile, text);
        Assert.That(text, Is.EqualTo(File.ReadAllText(GoldenFile).ReplaceLineEndings("\n")));
    }

    [Test]
    public async Task TheGoldenReadBackThroughTheReaderGivesTheEventsInOrder()
    {
        var written = await ScriptedAsync();

        var read = new List<SessionEvent>();
        await using (var stream = File.OpenRead(GoldenFile))
        await foreach (var streamEvent in SessionStreamReader.ReadAsync(stream, CancellationToken.None)) read.Add(streamEvent);

        Assert.That(read.Select(e => e.Type), Is.EqualTo(written.Select(w => w.EventType)));
        Assert.That(read.Select(e => e.Data.GetRawText()), Is.EqualTo(written.Select(w => w.Data)));
    }

    /// <summary>Every event of the scripted turns, in the order they were written.</summary>
    private static async Task<List<SseItem<string>>> ScriptedAsync()
    {
        var events = new List<SseItem<string>>();

        // A turn that thinks, says something, calls a tool and answers.
        events.AddRange(await TurnAsync(_ => new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new TextReasoningContent("The user attached a note; the tool can read it."),
                new TextContent("Let me check the note."),
                new FunctionCallContent("call_1", "ReadNote", new Dictionary<string, object?> { ["name"] = "recipe" }),
            ])),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "The note says: melktert on Sunday."))),
            "Read the recipe note.", toolsets: [new NoteTools()]));

        // A sub-session's event, as the specialist's stream reached the chat while its call ran.
        events.Add(ClaudeStyleStream.SubSession(new SubSessionEvent("call_sub", "puls", "message_start",
            """{"type":"message_start","message":{"session_id":"sess_peer","role":"assistant","toolsets":["web"]}}""")));

        // A command is answered without the model.
        events.AddRange(await TurnAsync(_ => new FakeChatClient(), "/tools"));

        // A turn cancelled while the model answers: the request is cut off, as a cancelled HTTP call is, and the stop says so.
        events.AddRange(await TurnAsync(cancel => new FakeChatClient(_ =>
        {
            cancel();
            throw new OperationCanceledException();
        }), "Cancel me."));

        // A model that fails: the error event, with what was charged.
        events.AddRange(await TurnAsync(_ => new FakeChatClient(_ => throw new InvalidOperationException("the model refused")),
            "Fail, please."));

        return events;
    }

    /// <summary>
    /// One turn, started as the endpoint starts it. The model is built from a cancel action for this turn, so a script can
    /// cancel the turn it is answering.
    /// </summary>
    private static async Task<List<SseItem<string>>> TurnAsync(Func<Action, FakeChatClient> model, string prompt,
        IEnumerable<object>? toolsets = null)
    {
        CancellationTokenSource? active = null;
        var engine = new AgentEngine(new CompositeSkillProvider(), model(() => active!.Cancel()), TestPaths.Agent(),
            toolsets: toolsets?.ToList() ?? []);
        var session = new AgentSession("sess_golden", engine, ["web"], 0, UsageTags.None);
        Assert.That(session.TryStartTurn(CancellationToken.None, out active), Is.True);

        var events = new List<SseItem<string>>();
        await foreach (var item in TurnStream.StreamTurnAsync(engine, session, prompt, active!)) events.Add(item);
        return events;
    }

    private static string Serialize(IEnumerable<SseItem<string>> events)
    {
        var text = new StringBuilder();
        foreach (var item in events) text.Append($"event: {item.EventType}\ndata: {item.Data}\n\n");
        return text.ToString();
    }
}
