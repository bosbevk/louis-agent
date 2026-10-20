using System.Net;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Text;
using Microsoft.AspNetCore.Components.Forms;
using ChatModel = LouisAgent.Web.Shared.Chats.Chat;

namespace LouisAgent.Web.Shared.Tests.Chats;

// W2: a turn's life, lifted out of the two chat pages into ChatRunner, pinned from what the pages did: a message opens
// a session and streams through TurnStream; the redraw is raised at most every 50 ms on a clock the test holds; stop
// asks the API; an error's text is the app's; attachments are read, capped at 1 MB; the chat is saved at the end.
public class ChatRunnerTests
{
    private sealed class Harness
    {
        public readonly ChatSettings Settings = new();
        public readonly RecordingJSRuntime Js = new();
        public readonly FakeClock Clock = new(new DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero));
        public readonly ChatStore Store;
        public readonly StubHandler Handler;
        public readonly ChatRunner Runner;
        public readonly List<string> Opened = [];
        public int Changed, Ended;

        public Harness(bool sessionGone = false, ChatText? text = null)
        {
            var api = FixtureApi.Client(Settings, out Handler, sessionGone);
            Store = new ChatStore(Js, Settings);
            Runner = new ChatRunner(api, Store, text ?? new ChatText(), () => api.CreateSessionAsync(["web"]), Clock);
            Runner.SessionOpened += id => Opened.Add(id);
            Runner.Changed += () => Changed++;
            Runner.TurnEnded += () => Ended++;
        }
    }

    [Test]
    public async Task AMessage_OpensASession_StreamsTheTurn_AndSavesTheChat()
    {
        var h = new Harness { Runner = { Draft = "  " + SessionStreamFixture.Question + "  " } };

        Assert.That(h.Runner.CanSend, Is.True);
        await h.Runner.SendAsync();

        Assert.That(h.Opened, Is.EqualTo(new[] { FixtureApi.SessionId }), "the page navigates to it");
        Assert.That(h.Runner.Chat, Is.Not.Null);
        Assert.That(h.Runner.Chat!.Title, Is.EqualTo(SessionStreamFixture.Question[..50] + "…"));
        Assert.That(h.Store.Chats, Is.EqualTo(new[] { h.Runner.Chat }), "first in the store");
        Assert.That(h.Runner.Draft, Is.Empty);
        Assert.That(h.Runner.Streaming, Is.False);

        var turn = h.Runner.Chat.Turns.Single();
        Assert.That(turn.UserText, Is.EqualTo(SessionStreamFixture.Question), "trimmed");
        Assert.That(turn.Blocks.Where(b => b.Kind == BlockKind.Tool).Select(b => b.ToolName), Is.EqualTo(new[] { "StartSubSession" }));
        Assert.That(turn.Usage, Is.Not.Null);
        Assert.That(turn.Error, Is.Null);
        Assert.That(h.Js.Calls, Does.Contain("localStorage.setItem"), "saved when the stream ended");
        Assert.That(h.Ended, Is.EqualTo(1));
        Assert.That(h.Handler.Requests.Select(r => $"{r.Method} {r.Url}"), Is.EqualTo(new[]
        {
            "POST http://localhost/sessions", $"POST http://localhost/sessions/{FixtureApi.SessionId}/messages",
        }));
    }

    [Test]
    public async Task TheRedraw_IsRaisedAtMostEveryFiftyMilliseconds()
    {
        var frozen = new Harness { Runner = { Draft = "x" } };
        await frozen.Runner.SendAsync();
        // The clock never moves: once when the turn is added, once for the first event, once when it ended.
        Assert.That(frozen.Changed, Is.EqualTo(3));

        var ticking = new Harness { Runner = { Draft = "x" } };
        ticking.Runner.Changed += () => ticking.Clock.Advance(TimeSpan.FromMilliseconds(60));
        await ticking.Runner.SendAsync();
        int events = (await SessionStreamFixture.EventsAsync()).Count;
        Assert.That(ticking.Changed, Is.EqualTo(events + 2), "every event redraws when 60 ms pass between them");
    }

    [Test]
    public async Task Stop_AsksTheApiToCancel()
    {
        var h = new Harness { Runner = { Chat = new ChatModel { SessionId = FixtureApi.SessionId } } };

        await h.Runner.StopAsync();

        Assert.That(h.Handler.Requests.Single().Url, Is.EqualTo($"http://localhost/sessions/{FixtureApi.SessionId}/cancel"));
        Assert.That(h.Runner.Notice, Is.Null);
    }

    [Test]
    public async Task Stop_WithoutAChat_DoesNothing()
    {
        var h = new Harness();
        await h.Runner.StopAsync();
        Assert.That(h.Handler.Requests, Is.Empty);
    }

    [Test]
    public async Task AGoneSession_EndsTheChat_WithTheAppsWords()
    {
        var text = new ChatText { SessionGone = "Die sessie is weg." };
        var h = new Harness(sessionGone: true, text) { Runner = { Chat = new ChatModel { SessionId = FixtureApi.SessionId }, Draft = "nog iets" } };

        await h.Runner.SendAsync();

        Assert.That(h.Runner.Chat!.Ended, Is.True);
        Assert.That(h.Runner.Chat.Turns.Single().Error, Is.EqualTo("Die sessie is weg."));
        Assert.That(h.Runner.CanSend, Is.False, "an ended chat takes no more messages");
        Assert.That(h.Js.Calls, Does.Contain("localStorage.setItem"), "saved all the same");
    }

    [Test]
    public void Describe_UsesTheAppsWords_ForTheErrorsItKnows()
    {
        var text = new ChatText { NeedsKey = "Sleutel!", StillAnswering = "Wag!", ApiUnreachable = "Weg!" };
        var h = new Harness(text: text);

        Assert.That(h.Runner.Describe(new AgentApiException(HttpStatusCode.Unauthorized, "401")), Is.EqualTo("Sleutel!"));
        Assert.That(h.Runner.Describe(new AgentApiException(HttpStatusCode.Conflict, "409")), Is.EqualTo("Wag!"));
        Assert.That(h.Runner.Describe(new AgentApiException(HttpStatusCode.BadRequest, "Unknown toolset(s): x")), Is.EqualTo("Unknown toolset(s): x"));
        Assert.That(h.Runner.Describe(new HttpRequestException("socket")), Is.EqualTo("Weg!"));
        Assert.That(h.Runner.Describe(new InvalidOperationException("boom")), Is.EqualTo("boom"));
    }

    private sealed class FakeFile(string name, string content, long? size = null) : IBrowserFile
    {
        public string Name => name;
        public DateTimeOffset LastModified => DateTimeOffset.MinValue;
        public long Size => size ?? content.Length;
        public string ContentType => "text/plain";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) =>
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
    }

    [Test]
    public async Task Attachments_AreRead_AndALargeOneIsRefusedWithANotice()
    {
        var h = new Harness();
        var files = new InputFileChangeEventArgs([new FakeFile("notes.md", "# Notes"), new FakeFile("huge.log", "", size: ChatRunner.MaxAttachmentBytes + 1)]);

        await h.Runner.AddFilesAsync(files);

        Assert.That(h.Runner.Attachments, Is.EqualTo(new[] { new Attachment("notes.md", "# Notes") }));
        Assert.That(h.Runner.Notice, Is.EqualTo("huge.log is larger than 1 MB and was not attached."));
        Assert.That(h.Runner.CanSend, Is.True, "an attachment alone can be sent");
    }

    [Test]
    public async Task AnAttachmentAlone_NamesTheChat_AndGoesWithTheTurn()
    {
        var h = new Harness();
        h.Runner.Attachments.Add(new Attachment("notes.md", "# Notes"));

        await h.Runner.SendAsync();

        Assert.That(h.Runner.Chat!.Title, Is.EqualTo("notes.md"));
        Assert.That(h.Runner.Chat.Turns.Single().AttachmentNames, Is.EqualTo(new[] { "notes.md" }));
        Assert.That(h.Runner.Attachments, Is.Empty);
        Assert.That(h.Handler.Requests.Last().Body, Does.Contain("\"name\":\"notes.md\"").And.Contain("# Notes"));
    }

    [Test]
    public void TitleFor_IsTheFirstLine_OrTheAttachment_OrTheAppsNewChat()
    {
        var h = new Harness(text: new ChatText { NewChat = "Nuwe gesprek" });

        Assert.That(h.Runner.TitleFor("Hallo\ndaar", []), Is.EqualTo("Hallo daar"));
        Assert.That(h.Runner.TitleFor("", [new Attachment("a.txt", "")]), Is.EqualTo("a.txt"));
        Assert.That(h.Runner.TitleFor("", []), Is.EqualTo("Nuwe gesprek"));
        Assert.That(h.Runner.TitleFor(new string('x', 60), []), Is.EqualTo(new string('x', 50) + "…"));
    }

    [Test]
    public async Task AFailedSessionOpen_KeepsTheDraft_AndSaysWhy()
    {
        var h = new Harness { Runner = { Draft = "Hallo" } };
        var failing = new ChatRunner(FixtureApi.Client(h.Settings), h.Store, new ChatText(), () => throw new HttpRequestException("down"), h.Clock)
        {
            Draft = "Hallo",
        };
        failing.Attachments.Add(new Attachment("a.txt", "x"));

        await failing.SendAsync();

        Assert.That(failing.Chat, Is.Null);
        Assert.That(failing.Draft, Is.EqualTo("Hallo"), "the message comes back to the box");
        Assert.That(failing.Attachments, Has.Count.EqualTo(1));
        Assert.That(failing.Notice, Is.EqualTo("Can't reach the agent API. Is the api container running?"));
    }

    [Test]
    public async Task DeletingAChat_RemovesItHereAndThere_AndSaysWhenItWasTheOneShown()
    {
        var h = new Harness();
        var shown = new ChatModel { SessionId = FixtureApi.SessionId };
        var other = new ChatModel { SessionId = "sess_other" };
        h.Store.Chats.AddRange([shown, other]);
        h.Runner.Chat = shown;

        Assert.That(await h.Runner.DeleteChatAsync(other), Is.False);
        Assert.That(await h.Runner.DeleteChatAsync(shown), Is.True, "the page leaves it");

        Assert.That(h.Store.Chats, Is.Empty);
        Assert.That(h.Runner.Chat, Is.Null);
        Assert.That(h.Handler.Requests.Select(r => r.Method.Method), Is.EqualTo(new[] { "DELETE", "DELETE" }), "a 404 for the other is swallowed");
    }

    [Test]
    public async Task SavingTheApiKey_ClearsTheNotice()
    {
        var h = new Harness { Runner = { Notice = "The API needs a key" } };

        await h.Runner.SaveApiKeyAsync(" k-1 ");

        Assert.That(h.Settings.ApiKey, Is.EqualTo("k-1"));
        Assert.That(h.Runner.Notice, Is.Null);
    }
}
