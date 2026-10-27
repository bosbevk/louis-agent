using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Components.Admin.Notes;
using LouisAgent.Testing;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Components.Admin.Notes;

/// <summary>The notes panel: it says so when there are none, lists them newest first when there are, and its form posts a note and then says it changed so the run page fetches again. It never adds the note to its own list.</summary>
public class NotesPanelTests
{
    /// <summary>The moment the tests count from.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);
    /// <summary>A conversation key.</summary>
    private static readonly ConversationRef Bobotie = new("run1", SampleMode.Beta, 1, "bobotie");

    /// <summary>A rendered panel with its stub API and state, and how often the state was told a note was written.</summary>
    private sealed record Rendered(IRenderedComponent<NotesPanel> Cut, StubHandler Handler, RunNotesState State, Func<int> Raised);

    /// <summary>Renders the panel with the given notes and an API that answers the post with the given status.</summary>
    private static Rendered Render(IReadOnlyList<RunNoteEntry> notes, ConversationRef? conversation = null, HttpStatusCode post = HttpStatusCode.Created)
    {
        var ctx = new BunitContext();

        // An API that answers every post with the status we were given; a refusal carries the reason the panel should show.
        var handler = new StubHandler(_ => new HttpResponseMessage(post)
        {
            Content = new StringContent(post == HttpStatusCode.Created ? """{"text":"x"}""" : """{"type":"error","error":{"type":"invalid_request_error","message":"A note needs some text."}}""",
                Encoding.UTF8, "application/json"),
        });
        ctx.Services.AddSingleton(new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, new ChatSettings()));

        // The shared state, counting how often the panel tells it a note was written.
        var state = new RunNotesState();
        int raised = 0;
        state.Changed += () => raised++;
        ctx.Services.AddSingleton(state);

        var cut = ctx.Render<NotesPanel>(p => p.Add(x => x.RunId, "run1").Add(x => x.Notes, notes).Add(x => x.Conversation, conversation));
        return new Rendered(cut, handler, state, () => raised);
    }

    /// <summary>A note a number of minutes after T0.</summary>
    private static RunNoteEntry Note(string text, int minutes, ConversationRef? conversation = null) => new(text, T0.AddMinutes(minutes), conversation);

    /// <summary>With no notes the panel says so, and still offers the form.</summary>
    [Test]
    public void WithNoNotes_ItSaysSo_AndOffersTheForm()
    {
        var (cut, _, _, _) = Render([]);

        Assert.That(cut.Find(".notes-empty").TextContent, Does.Contain("No notes yet"));
        Assert.That(cut.FindAll(".note"), Is.Empty);
        Assert.That(cut.Find(".note-text").TagName, Is.EqualTo("TEXTAREA"));
        Assert.That(cut.Find(".note-text").GetAttribute("maxlength"), Is.EqualTo("2000"));
        Assert.That(cut.Find(".note-add").HasAttribute("disabled"), Is.True, "nothing typed yet");
    }

    /// <summary>Two notes are listed newest first, each with its text and when it was written.</summary>
    [Test]
    public void WithTwoNotes_ItListsThemNewestFirst_EachWithItsTime()
    {
        var (cut, _, _, _) = Render([Note("judge changed", 1), Note("re-ran after the fix", 2)]);

        var rows = cut.FindAll(".note");
        Assert.That(rows.Select(r => r.QuerySelector(".note-body")!.TextContent), Is.EqualTo(new[] { "re-ran after the fix", "judge changed" }));
        Assert.That(rows[0].QuerySelector(".note-when")!.TextContent, Is.EqualTo(NoteText.When(T0.AddMinutes(2))));
        Assert.That(cut.FindAll(".notes-empty"), Is.Empty);
    }

    /// <summary>A conversation's panel shows only its own notes, and the run's only the run's.</summary>
    [Test]
    public void AConversationsPanel_ShowsOnlyItsOwnNotes_AndTheRunsPanelOnlyTheRuns()
    {
        var notes = new[] { Note("on the run", 1), Note("on bobotie", 2, Bobotie) };

        Assert.That(Render(notes, Bobotie).Cut.FindAll(".note-body").Select(n => n.TextContent), Is.EqualTo(new[] { "on bobotie" }));
        Assert.That(Render(notes).Cut.FindAll(".note-body").Select(n => n.TextContent), Is.EqualTo(new[] { "on the run" }));
    }

    /// <summary>The form posts the text, then tells the state. It clears the box, and doesn't put the note in its own list.</summary>
    [Test]
    public void TheForm_PostsTheNote_ThenSaysTheStoreChanged_WithoutAddingItToItsOwnList()
    {
        var (cut, handler, _, raised) = Render([]);

        cut.Find(".note-text").Input("the prompt changed here");
        cut.Find(".note-add").Click();

        cut.WaitForAssertion(() => Assert.That(raised(), Is.EqualTo(1), "the page is told, so it fetches the run again"));
        var (method, url, body) = handler.Requests.Single();
        Assert.That((method, url), Is.EqualTo((HttpMethod.Post, "http://localhost/v1/admin/benchmarks/runs/run1/notes")));

        // What was sent: the text, and no conversation for a note on the run.
        using var sent = JsonDocument.Parse(body!);
        Assert.That(sent.RootElement.GetProperty("text").GetString(), Is.EqualTo("the prompt changed here"));
        Assert.That(sent.RootElement.GetProperty("conversation").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(cut.FindAll(".note"), Is.Empty, "it shows what it was given, and nothing more: the page's new copy brings the note");
        Assert.That(cut.Find(".note-text").GetAttribute("value") ?? cut.Find(".note-text").TextContent, Is.Empty.Or.Null);
    }

    /// <summary>A note on a conversation posts that conversation's key.</summary>
    [Test]
    public void AConversationsForm_PostsItsKey()
    {
        var (cut, handler, _, raised) = Render([], Bobotie);

        cut.Find(".note-text").Input("hallucinated a price");
        cut.Find(".note-add").Click();

        cut.WaitForAssertion(() => Assert.That(raised(), Is.EqualTo(1)));
        using var sent = JsonDocument.Parse(handler.Requests.Single().Body!);
        var key = sent.RootElement.GetProperty("conversation");
        Assert.That((key.GetProperty("mode").GetString(), key.GetProperty("repeat").GetInt32(), key.GetProperty("task").GetString()), Is.EqualTo((SampleMode.Beta, 1, "bobotie")));
    }

    /// <summary>When the API says no, the panel shows why, keeps what was typed, and doesn't say anything changed.</summary>
    [Test]
    public void WhenTheApiRefuses_ItShowsTheReason_KeepsTheText_AndDoesNotSayTheStoreChanged()
    {
        var (cut, _, _, raised) = Render([], post: HttpStatusCode.BadRequest);

        cut.Find(".note-text").Input("a note");
        cut.Find(".note-add").Click();

        cut.WaitForAssertion(() => Assert.That(cut.Find(".note-error").TextContent, Does.Contain("A note needs some text.")));
        Assert.That(raised(), Is.EqualTo(0));
        Assert.That(cut.Find(".note-add").HasAttribute("disabled"), Is.False, "the text is still there to send again");
    }
}
