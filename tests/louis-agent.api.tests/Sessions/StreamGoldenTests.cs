using LouisAgent.Core.Agent.Toolsets;
using System.Net.ServerSentEvents;
using LouisAgent.Api.Sessions;
using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Usage.Records;
using LouisAgent.SessionStream;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace LouisAgent.Api.Tests.Sessions;

// F18 step 1e, the stream's golden before the seam (CL18 step 0): one scripted turn of each kind (a model's thinking,
// text and tool call, a sub-session's event, a command reply, a cancel and an error, with usage on the turns that call
// the model, and a failure with a text block open) is written through the endpoint's own SseFormatter, from
// TurnStream.StreamTurnAsync, and pinned to StreamGolden.sse byte for byte. The golden is the proof a later change to the
// writer (the stream format seam, F18 step 1f) moves no byte. Regenerate with LOUIS_UPDATE_GOLDEN=1.
public class StreamGoldenTests
{
    private static readonly string GoldenFile = Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.api.tests", "Sessions", "StreamGolden.sse");

    /// <summary>The one tool of the scripted turns: a note by name, as the ACP transcript's tool is.</summary>
    private sealed class NoteTools
    {
        [System.ComponentModel.Description("Reads a note by name")]
        public string ReadNote([System.ComponentModel.Description("The note's name")] string name) => $"note {name}: melktert, Sunday";
    }

    /// <summary>
    /// A model whose second request streams some text and then fails, as a dropped connection does, so the turn fails with
    /// its text block open. The first request is answered by the scripted client.
    /// </summary>
    private sealed class DropsAfterText(IChatClient first) : IChatClient
    {
        private int _streams;

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
            CancellationToken cancellationToken = default) => first.GetResponseAsync(messages, options, cancellationToken);

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
            ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (++_streams == 1)
            {
                await foreach (var update in first.GetStreamingResponseAsync(messages, options, cancellationToken)) yield return update;
                yield break;
            }
            yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextContent("Checking the second note")]);
            throw new InvalidOperationException("the connection dropped");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    [Test]
    public async Task TheStreamMatchesItsGolden()
    {
        byte[] written = await WrittenAsync(await ScriptedAsync());

        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1") await File.WriteAllBytesAsync(GoldenFile, written);
        Assert.That(written, Is.EqualTo(await File.ReadAllBytesAsync(GoldenFile)));
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

        // A turn that thinks, says something, calls a tool and answers; both requests carry their usage.
        events.AddRange(await TurnAsync(_ => new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new TextReasoningContent("The user attached a note; the tool can read it."),
                new TextContent("Let me check the note."),
                new FunctionCallContent("call_1", "ReadNote", new Dictionary<string, object?> { ["name"] = "recipe" }),
                Usage(120, 20),
            ])),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, [new TextContent("The note says: melktert on Sunday."), Usage(150, 12)]))),
            "Read the recipe note.", toolsets: [new NoteTools()]));

        // A sub-session's event, as the specialist's stream reached the chat while its call ran.
        events.Add(StreamFormats.Create(StreamFormat.Claude, "sess_golden", [CoreToolset.Web.Name()], null).SubSession(new SubSessionEvent("call_sub", "puls", "message_start",
            """{"type":"message_start","message":{"session_id":"sess_peer","role":"assistant","toolsets":["web"]}}""")));

        // A command is answered without the model.
        events.AddRange(await TurnAsync(_ => new FakeChatClient(), "/tools"));

        // A turn cancelled while the model answers: the request is cut off, as a cancelled HTTP call is, and the stop says so.
        events.AddRange(await TurnAsync(cancel => new FakeChatClient(_ =>
        {
            cancel();
            throw new OperationCanceledException();
        }), "Cancel me."));

        // A turn that fails on its second request, with a text block open: the block closes, then the error, with the usage
        // of the request that answered before it.
        events.AddRange(await TurnAsync(_ => new DropsAfterText(new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("call_2", "ReadNote", new Dictionary<string, object?> { ["name"] = "recipe" }),
                Usage(80, 5),
            ])))), "Fail, please.", toolsets: [new NoteTools()]));

        return events;
    }

    /// <summary>
    /// One turn, started as the endpoint starts it, on an engine that records its usage to a sink without prices, so the
    /// usage in the bytes doesn't depend on the price table. The model is built from a cancel action for this turn, so a
    /// script can cancel the turn it is answering.
    /// </summary>
    private static async Task<List<SseItem<string>>> TurnAsync(Func<Action, IChatClient> model, string prompt,
        IEnumerable<object>? toolsets = null)
    {
        CancellationTokenSource? active = null;
        var engine = new AgentEngine(new CompositeSkillProvider(), model(() => active!.Cancel()), TestPaths.Agent(),
            toolsets: toolsets?.ToList() ?? [], usageSink: new ListUsageSink());
        var session = new AgentSession("sess_golden", engine, [CoreToolset.Web.Name()], 0, UsageTags.None);
        Assert.That(session.TryStartTurn(CancellationToken.None, out active), Is.True);

        var events = new List<SseItem<string>>();
        await foreach (var item in TurnStream.StreamTurnAsync(engine, session, prompt, active!)) events.Add(item);
        return events;
    }

    /// <summary>A request's usage, as a provider reports it in its stream: unpriced, so no price table is read.</summary>
    private static UsageContent Usage(long input, long output) => new(new Microsoft.Extensions.AI.UsageDetails
    {
        InputTokenCount = input,
        OutputTokenCount = output,
    });

    /// <summary>The bytes a client gets: the endpoint writes its events with SseFormatter, so the golden is written that way too.</summary>
    private static async Task<byte[]> WrittenAsync(IEnumerable<SseItem<string>> events)
    {
        using var stream = new MemoryStream();
        await SseFormatter.WriteAsync(AsAsync(events), stream, CancellationToken.None);
        return stream.ToArray();
    }

    private static async IAsyncEnumerable<SseItem<string>> AsAsync(IEnumerable<SseItem<string>> events)
    {
        foreach (var item in events) yield return item;
        await Task.CompletedTask;
    }
}
