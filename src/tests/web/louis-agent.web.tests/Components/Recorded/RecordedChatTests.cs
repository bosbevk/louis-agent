using System.Net;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Testing;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Recorded;

/// <summary>
/// A recorded session plays in the chat's own view (F120): the site hands over the recording's address and the
/// question, the answer types out as it did, and the ending (the offer) shows once the play is over, with a button to
/// play it again.
/// </summary>
public class RecordedChatTests
{
    private const string Source = "recordings/session.sse";

    /// <summary>A context whose site serves the recorded Puls turn at <see cref="Source"/>, or nothing.</summary>
    private static BunitContext Context(bool served = true)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        var handler = new StubHandler(request => served && request.RequestUri!.AbsolutePath.EndsWith(Source)
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(SessionStreamFixture.StreamFile)),
            }
            : new HttpResponseMessage(HttpStatusCode.NotFound));
        ctx.Services.AddSingleton(new HttpClient(handler) { BaseAddress = new Uri("http://site.test/") });
        return ctx;
    }

    /// <summary>One recorded turn: the fixture's question and its stream.</summary>
    private static readonly RecordedTurn[] OneTurn = [new(SessionStreamFixture.Question, Source)];

    /// <summary>The recording with no waits, the question asked, and an ending that names itself.</summary>
    private static IRenderedComponent<RecordedChat> Render(BunitContext ctx) =>
        ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Turns, OneTurn)
            .Add(chat => chat.Title, "the agent at work")
            .Add(chat => chat.PlayAgain, "Play again")
            .Add(chat => chat.Pace, 0)
            .Add(chat => chat.Ending, Offer));

    /// <summary>The site's ending: an offer the tests can find.</summary>
    private static readonly RenderFragment Offer =
        builder => builder.AddMarkupContent(0, "<p class=\"offer\">Yours?</p>");

    /// <summary>The answer the recording ends on.</summary>
    private static async Task<string> AnswerAsync() =>
        (await SessionStreamFixture.ReplayAsync()).Blocks.Last(block => block.Kind == BlockKind.Text).Text;

    /// <summary>
    /// Each tool call in the recording is told to the page as it plays (F127.1), so a showroom's mock website can go
    /// where its agent's ShowPage call says.
    /// </summary>
    [Test]
    public async Task EachToolCall_IsToldToThePage()
    {
        using var ctx = Context();
        var told = new List<string?>();
        var expected = (await SessionStreamFixture.ReplayAsync()).Blocks.Where(block => block.Kind == BlockKind.Tool)
            .Select(block => block.ToolUseId).ToList();

        var cut = ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Turns, OneTurn)
            .Add(chat => chat.Pace, 0)
            .Add(chat => chat.ToolCalled, (Block call) => told.Add(call.ToolUseId)));
        cut.WaitForElement(".recorded-end");

        Assert.That(told, Is.EqualTo(expected).And.Not.Empty);
    }

    /// <summary>The question shows, and the recorded answer types out in the chat's own view.</summary>
    [Test]
    public async Task TheRecording_PlaysInTheChatsView()
    {
        using var ctx = Context();
        string answer = await AnswerAsync();

        var cut = Render(ctx);

        cut.WaitForAssertion(() => Assert.That(cut.Find(".recorded-chat .messages").TextContent,
            Does.Contain(answer[..40])));
        Assert.That(cut.Markup, Does.Contain("the agent at work"));
    }

    /// <summary>
    /// FX194: a way to try the agent live is the theme's own button, drawn by the chat beside Play again, once the
    /// play is over. The site gave its own unstyled button before, which showed as the browser's grey box.
    /// </summary>
    [Test]
    public void TryingItLive_IsTheThemesButton_OnceThePlayIsOver()
    {
        using var ctx = Context();
        int tried = 0;

        var cut = ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Turns, [new RecordedTurn("Hello", Source)])
            .Add(chat => chat.PlayAgain, "Play again")
            .Add(chat => chat.Pace, 0)
            .Add(chat => chat.TryIt, "Try it yourself")
            .Add(chat => chat.Tried, () => tried++));
        var button = cut.WaitForElement(".recorded-end button.btn.btn-fill.btn-small", TimeSpan.FromSeconds(5));
        button.Click();

        Assert.Multiple(() =>
        {
            Assert.That(button.TextContent, Is.EqualTo("Try it yourself"));
            Assert.That(tried, Is.EqualTo(1));
        });
    }

    /// <summary>The ending waits for the play to finish, and play again starts it over.</summary>
    [Test]
    public void TheEnding_ShowsWhenThePlayIsOver_AndPlaysAgain()
    {
        using var ctx = Context();

        var cut = Render(ctx);
        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".offer"), Has.Count.EqualTo(1)));
        cut.Find(".recorded-again").Click();

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".offer"), Has.Count.EqualTo(1)));
        Assert.That(cut.FindAll(".recorded-chat .turn").Count, Is.LessThanOrEqualTo(1), "one turn, played again");
    }

    /// <summary>
    /// Played from a later turn (F127.1, Louis: "this should be clickable so visitors can rewind"), the turns before it
    /// show at once, each told as it starts, and the chosen turn plays at its pace.
    /// </summary>
    [Test]
    public async Task PlayedFromATurn_TheTurnsBefore_ShowAtOnce()
    {
        using var ctx = Context();
        string answer = await AnswerAsync();
        var started = new List<int>();
        var cut = ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Turns,
                [new RecordedTurn("First question", Source), new RecordedTurn("Second one", Source)])
            .Add(chat => chat.Pace, 50)
            .Add(chat => chat.Ending, Offer)
            .Add(chat => chat.TurnStarted, (int turn) => started.Add(turn)));

        _ = cut.InvokeAsync(() => cut.Instance.PlayFromAsync(2));

        cut.WaitForAssertion(() =>
        {
            Assert.That(cut.FindAll(".recorded-chat .turn"), Has.Count.EqualTo(2));
            Assert.That(cut.FindAll(".recorded-chat .turn")[0].TextContent, Does.Contain(answer[..40]));
        });
        Assert.That((started.TakeLast(2), cut.FindAll(".offer").Count), Is.EqualTo((new[] { 1, 2 }, 0)),
            "the first turn at once, the second still playing at its pace");
    }

    /// <summary>The bill shows in the site's currency, as the live chat's does, when the site gives one.</summary>
    [Test]
    public void TheBill_ShowsInTheSitesCurrency()
    {
        using var ctx = Context();

        var cut = ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Turns, OneTurn)
            .Add(chat => chat.Pace, 0)
            .Add(chat => chat.Currency, new CurrencyChoice("ZAR", "R", 18m, "2026-11-06", "USD")));

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".recorded-end"), Has.Count.EqualTo(1)));
        Assert.That(cut.Find(".recorded-chat .messages").TextContent, Does.Contain("R"));
        Assert.That(cut.Find(".recorded-chat .messages").TextContent, Does.Not.Contain("$"));
    }

    /// <summary>
    /// A conversation plays its turns in order (F128.2): each question, then the answer recorded for it, the next only
    /// once the last is done, as a booking goes, then the ending.
    /// </summary>
    [Test]
    public void AConversation_PlaysItsTurnsInOrder()
    {
        using var ctx = Context();

        var cut = ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Turns,
                [new RecordedTurn("First question", Source), new RecordedTurn("Second one", Source)])
            .Add(chat => chat.Pace, 0)
            .Add(chat => chat.Ending, Offer));

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".offer"), Has.Count.EqualTo(1)));
        Assert.That(cut.FindAll(".recorded-chat .turn .user-text").Select(text => text.TextContent),
            Is.EqualTo(new[] { "First question", "Second one" }));
    }

    /// <summary>
    /// A customer's view hides the agent's work, as a customer's site does: its words and a thinking label.
    /// </summary>
    [Test]
    public void WithItsWorkHidden_ARecordingShowsTheWordsAlone()
    {
        using var ctx = Context();

        var cut = ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Turns, OneTurn)
            .Add(chat => chat.Pace, 0)
            .Add(chat => chat.ShowWork, false));

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".recorded-end"), Has.Count.EqualTo(1)));
        Assert.That(cut.FindAll(".recorded-chat .tool, .recorded-chat .delegation"), Is.Empty);
    }

    /// <summary>
    /// Each turn says when it starts, by its number from one (F127.1), so a showroom's cues light in step with the
    /// play.
    /// </summary>
    [Test]
    public void EachTurn_SaysWhenItStarts()
    {
        using var ctx = Context();
        var started = new List<int>();

        var cut = ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Turns, [OneTurn[0], OneTurn[0]])
            .Add(chat => chat.Pace, 0)
            .Add(chat => chat.TurnStarted, (int turn) => started.Add(turn)));

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".recorded-end"), Has.Count.EqualTo(1)));
        Assert.That(started, Is.EqualTo(new[] { 1, 2 }));
    }

    /// <summary>
    /// With a gap between turns (F127.1, Louis: "slow it down like realtime"), the next question waits, as a person
    /// reads the answer and types, instead of following it at once.
    /// </summary>
    [Test]
    public void AGapBetweenTurns_HoldsTheNextQuestion()
    {
        using var ctx = Context();

        var cut = ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Turns, [OneTurn[0], OneTurn[0]])
            .Add(chat => chat.Pace, 0)
            .Add(chat => chat.TurnGap, 60_000));

        cut.WaitForAssertion(() => Assert.That(cut.Markup, Does.Contain("recorded")));
        Thread.Sleep(400);
        Assert.That(cut.FindAll(".recorded-end"), Is.Empty, "the second turn still waits");
    }

    /// <summary>A recording the site can't serve shows the ending at once, never an empty window that waits.</summary>
    [Test]
    public void AMissingRecording_GoesStraightToTheEnding()
    {
        using var ctx = Context(served: false);

        var cut = Render(ctx);

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".offer"), Has.Count.EqualTo(1)));
    }

    /// <summary>
    /// Held (F134.4), the play doesn't move on: not even its question shows, and once let go it goes on from there.
    /// </summary>
    [Test]
    public void AHeldPlay_WaitsUntilLetGo()
    {
        using var ctx = Context();
        var cut = ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Turns, OneTurn)
            .Add(chat => chat.Pace, 0)
            .Add(chat => chat.Hold, true)
            .Add(chat => chat.Ending, Offer));

        Thread.Sleep(400);
        bool askedWhileHeld = cut.Markup.Contains(SessionStreamFixture.Question, StringComparison.Ordinal);
        cut.Render(parameters => parameters.Add(chat => chat.Hold, false));

        // Its question shows as soon as the play goes on. A wait for a state, not an assertion: NUnit counts each
        // failed try inside a retried assertion against the test, even when a later try passes.
        cut.WaitForState(() => cut.Markup.Contains(SessionStreamFixture.Question, StringComparison.Ordinal),
            TimeSpan.FromSeconds(5));

        Assert.That(askedWhileHeld, Is.False, "the question waited while the play was held");
    }

    /// <summary>
    /// Started when asked (F160, Louis: "do not make it autoplay"), nothing plays: the first question waits with a
    /// Play button, the theme's main one, and a press plays the talk to its end.
    /// </summary>
    [Test]
    public void StartedWhenAsked_TheFirstQuestionWaits_UntilPlayIsPressed()
    {
        using var ctx = Context();

        var cut = ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Turns, [new RecordedTurn("First question", Source), new RecordedTurn("Second", Source)])
            .Add(chat => chat.Pace, 0)
            .Add(chat => chat.Start, RecordedStart.WhenAsked)
            .Add(chat => chat.Play, "Play the talk")
            .Add(chat => chat.Ending, Offer));
        Thread.Sleep(400);
        var waiting = cut.FindAll(".recorded-chat .turn .user-text").Select(text => text.TextContent).ToList();
        bool ended = cut.FindAll(".offer").Count > 0;
        var play = cut.Find(".recorded-wait button.btn.btn-fill.btn-small");
        string playWords = play.TextContent;
        play.Click();

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".offer"), Has.Count.EqualTo(1)), TimeSpan.FromSeconds(5));
        Assert.Multiple(() =>
        {
            Assert.That(waiting, Is.EqualTo(new[] { "First question" }));
            Assert.That(ended, Is.False);
            Assert.That(playWords, Is.EqualTo("Play the talk"));
            Assert.That(cut.FindAll(".recorded-chat .turn .user-text"), Has.Count.EqualTo(2));
        });
    }

    /// <summary>Three turns, started when asked, with no waits and the theme's words for each button.</summary>
    private static IRenderedComponent<RecordedChat> ThreeTurns(BunitContext ctx, bool held = false) =>
        ctx.Render<RecordedChat>(parameters => parameters
            .Add(chat => chat.Turns,
                [new RecordedTurn("First", Source), new RecordedTurn("Second", Source), new RecordedTurn("Third", Source)])
            .Add(chat => chat.Pace, 0)
            .Add(chat => chat.Start, RecordedStart.WhenAsked)
            .Add(chat => chat.Play, "Play")
            .Add(chat => chat.PlayOn, "Play on")
            .Add(chat => chat.Hold, held)
            .Add(chat => chat.Ending, Offer));

    /// <summary>
    /// Asked for one turn (F160, a turn is a stop the guide shows), the turns before it show whole, that turn plays,
    /// and the play stops after it with Play on, which plays the rest.
    /// </summary>
    [Test]
    public void AskedForATurn_ItPlays_AndStopsAfterIt()
    {
        using var ctx = Context();
        var started = new List<int>();
        var cut = ThreeTurns(ctx);
        cut.Render(parameters => parameters.Add(chat => chat.TurnStarted, (int turn) => started.Add(turn)));

        _ = cut.InvokeAsync(() => cut.Instance.PlayTurnAsync(2));
        cut.WaitForElement(".recorded-wait .btn-fill", TimeSpan.FromSeconds(5));
        var stopped = cut.FindAll(".recorded-chat .turn .user-text").Select(text => text.TextContent).ToList();
        var playOn = cut.Find(".recorded-wait .btn-fill");
        string playOnWords = playOn.TextContent;
        bool canPlayAgain = cut.FindAll(".recorded-wait .recorded-again").Count == 1;
        bool endedEarly = cut.FindAll(".offer").Count > 0;
        playOn.Click();

        cut.WaitForElement(".offer", TimeSpan.FromSeconds(5));
        Assert.Multiple(() =>
        {
            Assert.That(stopped, Is.EqualTo(new[] { "First", "Second" }));
            Assert.That((playOnWords, canPlayAgain, endedEarly), Is.EqualTo(("Play on", true, false)));
            Assert.That(started.Take(2), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(cut.FindAll(".recorded-chat .turn"), Has.Count.EqualTo(3));
        });
    }

    /// <summary>
    /// Asked for a turn while the guide talks, the window keeps what it shows and the turn waits until let go, so the
    /// visitor reads the guide's line first.
    /// </summary>
    [Test]
    public void AskedForATurnWhileHeld_ItWaitsUntilLetGo()
    {
        using var ctx = Context();
        var cut = ThreeTurns(ctx, held: true);

        _ = cut.InvokeAsync(() => cut.Instance.PlayTurnAsync(2));
        Thread.Sleep(400);
        var whileHeld = cut.FindAll(".recorded-chat .turn .user-text").Select(text => text.TextContent).ToList();
        cut.Render(parameters => parameters.Add(chat => chat.Hold, false));

        cut.WaitForState(() => cut.FindAll(".recorded-wait .btn-fill").Any(button => button.TextContent == "Play on"),
            TimeSpan.FromSeconds(5));
        Assert.That(whileHeld, Is.EqualTo(new[] { "First" }), "the first question still waits, nothing played");
    }
}
