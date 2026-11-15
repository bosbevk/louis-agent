using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Components.Widget.Bar;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Bar;

/// <summary>
/// F134.3: the bar holds the orb, a line of what the guide is doing and the box. Over it stands the guide's last
/// answer, and the whole talk once the visitor opens it.
/// </summary>
public class AgentBarTests
{
    /// <summary>A context with what the widget asks for, over the stub API.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out _);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>The guide's widget as a bar.</summary>
    private static IRenderedComponent<ChatWidget> Bar(BunitContext ctx) =>
        ctx.Render<ChatWidget>(parameters => parameters
            .Add(w => w.Identity, "guide")
            .Add(w => w.Title, "Louis · AI")
            .Add(w => w.Placeholder, "Your question…")
            .Add(w => w.Layout, WidgetLayout.Bar));

    /// <summary>
    /// Before anything is asked the bar is the orb, idle, the guide's name where its doing goes, and the box; there
    /// is no talk yet.
    /// </summary>
    [Test]
    public void Waiting_TheBarIsTheOrbItsNameAndTheBox()
    {
        using var ctx = Context();

        var cut = Bar(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".agent-bar .orb").ClassList, Does.Contain("is-idle"));
            Assert.That(cut.Find(".agent-bar .orb").TextContent.Trim(), Is.EqualTo("L"), "the guide's initial");
            Assert.That(cut.Find(".agent-doing").TextContent, Is.EqualTo("Louis · AI"));
            Assert.That(cut.Find("label.agent-doing").GetAttribute("for"), Is.EqualTo("composer-input"));
            Assert.That(cut.FindAll(".agent-box #composer-input"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll(".agent-box .composer-tools button.send"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll(".agent-talk"), Is.Empty);
        });
    }

    /// <summary>
    /// Once a question has an answer, the answer stands over the box as a caption, with no talk drawn, and the
    /// caption's button opens the whole talk in its place.
    /// </summary>
    [Test]
    public void AfterAnAnswer_ItStandsAsACaption_AndItsButtonOpensTheWholeTalk()
    {
        using var ctx = Context();
        var cut = Bar(ctx);

        cut.Find("#composer-input").Input("What is this site?");
        cut.Find("button.send").Click();
        // The answer has played out when the orb is idle again, the stream's redraws done.
        cut.WaitForState(() => cut.FindAll(".agent-caption").Count == 1 && cut.FindAll(".orb.is-idle").Count == 1,
            TimeSpan.FromSeconds(10));
        int talkAtFirst = cut.FindAll(".agent-talk").Count;
        cut.Find(".caption-talk").Click();
        cut.WaitForState(() => cut.FindAll(".agent-talk").Count == 1, TimeSpan.FromSeconds(2));

        Assert.That((talkAtFirst, cut.FindAll(".agent-caption").Count), Is.EqualTo((0, 0)));
    }

    /// <summary>A turn ends in words only when the last thing in it is text with something in it.</summary>
    [Test]
    public void ATurn_EndsInWords_OnlyWhenTextCameLast()
    {
        var thinking = new Turn { Blocks = [new Block { Kind = BlockKind.Thinking, Text = "…" }] };
        var answered = new Turn { Blocks = [new Block { Kind = BlockKind.Text, Text = "Hello." }] };
        var calling = new Turn
        {
            Blocks = [new Block { Kind = BlockKind.Text, Text = "Let me look." }, new Block { Kind = BlockKind.Tool }]
        };

        Assert.That((thinking.EndsInWords, answered.EndsInWords, calling.EndsInWords),
            Is.EqualTo((false, true, false)));
    }

    /// <summary>
    /// The site's suggestions stand over the bar as chips while the guide waits, each asking its question, and end
    /// with the word that the box is there too (F134's rule 3). The first is the one the site suggests, and is drawn
    /// bright, as the design has one chip. They step aside while the guide works.
    /// </summary>
    [Test]
    public void TheSuggestions_AreChipsWhileItWaits_AndAChipAsks()
    {
        using var ctx = Context();
        ctx.Services.AddSingleton(new AgentSuggestions(["Walk me through this page", "What does it cost?"]));
        var cut = Bar(ctx);

        var chips = cut.FindAll(".agent-chips button").Select(b => b.TextContent).ToList();
        var suggested = cut.FindAll(".agent-chips button.is-suggested").Select(b => b.TextContent).ToList();
        var note = cut.Find(".agent-chips .agent-chips-note").TextContent;
        cut.Find(".agent-chips button").Click();
        // What was asked is the runner's to say: the bar draws the answer, and the question only in the whole talk.
        var runner = cut.FindComponent<AgentBar>().Instance.Runner;
        cut.WaitForState(() => runner.Chat is { Turns.Count: 1 }, TimeSpan.FromSeconds(10));
        var asked = runner.Chat!.Turns[0].UserText;

        Assert.Multiple(() =>
        {
            Assert.That(chips, Is.EqualTo(new[] { "Walk me through this page", "What does it cost?" }));
            Assert.That(suggested, Is.EqualTo(new[] { "Walk me through this page" }), "the first is the bright one");
            Assert.That(note, Is.EqualTo("or type your own"));
            Assert.That(asked, Is.EqualTo("Walk me through this page"));
        });
    }

    /// <summary>A site that names no suggestions has no chips.</summary>
    [Test]
    public void WithNoSuggestions_ThereAreNoChips()
    {
        using var ctx = Context();

        Assert.That(Bar(ctx).FindAll(".agent-chips"), Is.Empty);
    }

    /// <summary>
    /// On a phone the chips are one row that scrolls sideways (F134.3, looked at at 390 px): wrapped, three rows of
    /// them stood over the end of a page and the start's dock.
    /// </summary>
    [Test]
    public void OnAPhone_TheChipsAreOneRow()
    {
        string styles = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Widget/Bar/AgentBar.razor.scss")).ReplaceLineEndings();
        int phone = styles.IndexOf("@media (max-width: 900px) {", StringComparison.Ordinal);

        Assert.That(phone >= 0 ? styles[phone..] : "",
            Does.Contain("flex-wrap: nowrap;").And.Contain("overflow-x: auto;"));
    }

    /// <summary>
    /// The chips float over whatever the page shows, a business's light website included. Hovered, a chip once
    /// turned to see-through white and vanished over a light page. So a hovered chip keeps a dark glass of its own,
    /// and the note after the chips has a backing too.
    /// </summary>
    [Test]
    public void OverALightPage_AChipStaysReadable_HoveredToo()
    {
        string styles = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Widget/Bar/AgentBar.razor.scss")).ReplaceLineEndings("\n");
        int at = styles.IndexOf(".agent-chips button:hover {", StringComparison.Ordinal);
        string hovered = at < 0 ? "" : styles[at..styles.IndexOf('}', at)];
        int note = styles.IndexOf("\n.agent-chips-note {", StringComparison.Ordinal);
        string noted = note < 0 ? "" : styles[note..styles.IndexOf("\n}", note + 1, StringComparison.Ordinal)];

        Assert.Multiple(() =>
        {
            Assert.That(hovered, Does.Contain("background: rgba(var(--bs-secondary-bg-rgb)"));
            Assert.That(hovered, Does.Not.Contain("--web-overlay-rgb"), "never see-through white");
            Assert.That(noted, Does.Contain("background: rgba(var(--bs-body-bg-rgb)"));
        });
    }

    /// <summary>
    /// While the guide answers, the page is told it is talking, and once the answer is done that it has stopped
    /// (F134.4): a showroom's demo holds its play meanwhile.
    /// </summary>
    [Test]
    public void WhileTheGuideAnswers_ThePageIsToldItTalks_AndThenThatItStopped()
    {
        using var ctx = Context();
        var activity = new GuideActivity();
        ctx.Services.AddSingleton(activity);
        var told = new List<bool>();
        activity.Changed += () => told.Add(activity.Talking);
        var cut = Bar(ctx);

        cut.Find("#composer-input").Input("What is this site?");
        cut.Find("button.send").Click();
        cut.WaitForState(() => cut.FindAll(".agent-caption").Count == 1 && cut.FindAll(".orb.is-idle").Count == 1,
            TimeSpan.FromSeconds(10));

        // Waits for the last telling, then asserts once: NUnit counts a retried assertion's failed tries.
        cut.WaitForState(() => told.Count >= 2, TimeSpan.FromSeconds(2));

        Assert.That(told, Is.EqualTo(new[] { true, false }));
    }
}
