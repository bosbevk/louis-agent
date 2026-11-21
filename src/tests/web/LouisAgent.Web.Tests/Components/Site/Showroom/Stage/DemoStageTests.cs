using LouisAgent.Web.Components.Site.Showroom.Stage;
using LouisAgent.Web.Components.Site.Tiles;

namespace LouisAgent.Web.Tests.Components.Site.Showroom.Stage;

/// <summary>
/// F127.1: a demo page is a stage in its demo's colour, standing on the lit stage since F134.2: the topic, the title
/// as the page's one h1, the summary, then the player with what to watch, and the offer under them.
/// </summary>
public class DemoStageTests
{
    /// <summary>A stage in green with every slot filled, cues included unless told otherwise.</summary>
    private static IRenderedComponent<DemoStage> Stage(BunitContext ctx, bool cues = true) =>
        ctx.Render<DemoStage>(parameters =>
        {
            parameters
                .Add(stage => stage.Accent, TileAccent.Green)
                .Add(stage => stage.Topic, "A bakery")
                .Add(stage => stage.Title, "Bread at dawn.")
                .Add(stage => stage.Summary, "A baker's agent answers before the oven is warm.")
                .Add(stage => stage.Player, "<div class=\"player\">The chat</div>")
                .Add(stage => stage.Offer, "<a href=\"/contact\">Ask for yours</a>");
            if (cues) parameters.Add(stage => stage.Cues, "<ol class=\"cues\"><li>One</li></ol>");
        });

    /// <summary>The stage wears its demo's colour, and its title is the page's one h1, under the topic.</summary>
    [Test]
    public void TheStage_WearsItsColour_AndTheTitleIsTheH1()
    {
        using var ctx = new BunitContext();

        var cut = Stage(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".demo-stage").ClassList, Does.Contain("accent-green"));
            Assert.That(cut.Find(".stage-head .stage-topic").TextContent, Is.EqualTo("A bakery"));
            Assert.That(cut.FindAll("h1").Select(h => h.TextContent), Is.EqualTo(new[] { "Bread at dawn." }));
            Assert.That(cut.Find(".stage-head .stage-summary").TextContent, Does.StartWith("A baker's agent"));
        });
    }

    /// <summary>
    /// A demo's page stands on the start's stage (F134.2): the lit stage in the demo's colour holds the whole page, and
    /// the strip of the older look is gone.
    /// </summary>
    [Test]
    public void TheStage_StandsInTheLight_OfItsColour()
    {
        using var ctx = new BunitContext();
        var cut = Stage(ctx);

        var lit = cut.Find(".lit-stage");
        Assert.Multiple(() =>
        {
            Assert.That(lit.ClassList, Does.Contain("accent-green"));
            Assert.That(lit.QuerySelector(".demo-stage"), Is.Not.Null, "the page stands on it");
            Assert.That(cut.FindAll(".stage-strip"), Is.Empty);
        });
    }

    /// <summary>
    /// The player comes first, the whole width of the stage, and what to watch stands under it as chapters, as the K
    /// mockup has it (F134.2); the offer comes after both.
    /// </summary>
    [Test]
    public void ThePlayer_HasWhatToWatchUnderIt_AndTheOfferUnderBoth()
    {
        using var ctx = new BunitContext();

        var cut = Stage(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".demo-stage section[id]").Select(part => part.Id),
                Is.EqualTo(new[] { "top", "the-demo", "what-to-watch", "ask" }));
            Assert.That(cut.Find("#the-demo .player").TextContent, Is.EqualTo("The chat"));
            Assert.That(cut.Find(".stage-screen + .stage-offer a").GetAttribute("href"), Is.EqualTo("/contact"));
        });
    }

    /// <summary>
    /// The stage gives the guide's tour its places (F127.1, Louis: "make sure this works"), each a section with an id
    /// and a heading, so a walkthrough stops at the story, the demo, what to watch and the offer, one at a time.
    /// </summary>
    [Test]
    public void TheStage_GivesTheTourItsPlaces()
    {
        using var ctx = new BunitContext();

        var places = Stage(ctx).FindAll("section[id]")
            .Select(section => (section.Id, section.QuerySelector("h1, h2")?.TextContent.Trim())).ToList();

        Assert.That(places,
            Is.EqualTo(new[]
            {
                ("top", "Bread at dawn."), ("the-demo", "The demo"), ("what-to-watch", null),
                ("ask", "Ask for yours"),
            }), "the cues bring their own heading, so the stage writes none for them");
    }

    /// <summary>
    /// A stage with notes of what a custom agent could add sets them as a place of their own between the demo and the
    /// offer, so the guide's tour stops there too.
    /// </summary>
    [Test]
    public void Notes_StandBetweenTheDemoAndTheOffer()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<DemoStage>(parameters => parameters
            .Add(stage => stage.Accent, TileAccent.Green)
            .Add(stage => stage.Title, "Bread at dawn.")
            .Add(stage => stage.Player, "<div class=\"player\">The chat</div>")
            .Add(stage => stage.Notes, "<div class=\"notes\">Make it yours</div>")
            .Add(stage => stage.Offer, "<a href=\"/contact\">Ask for yours</a>"));

        Assert.That(cut.FindAll(".demo-stage section[id]")
                .Select(part => part.Id),
            Is.EqualTo(new[] { "top", "the-demo", "make-it-yours", "ask" }));
    }

    /// <summary>A demo without cues (its player shows its own steps) has no chapters under it.</summary>
    [Test]
    public void WithoutCues_NoChaptersStandUnderThePlayer()
    {
        using var ctx = new BunitContext();

        var cut = Stage(ctx, cues: false);

        Assert.That(cut.FindAll(".demo-stage section[id]").Select(part => part.Id),
            Is.EqualTo(new[] { "top", "the-demo", "ask" }));
    }

    /// <summary>
    /// The story, the demo and what to watch share the first screen, as the K mockup drew it: they stand in one frame
    /// that is as tall as the screen under the showroom's bar and over the guide's, and the player takes what the
    /// other two leave. The notes and the offer come under it.
    /// </summary>
    [Test]
    public void TheFirstScreen_HoldsTheStoryTheDemoAndWhatToWatch()
    {
        using var ctx = new BunitContext();
        string styles = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Components/Site/Showroom/Stage/DemoStage.razor.scss"));

        var screen = Stage(ctx).Find(".demo-stage > .stage-screen");

        Assert.Multiple(() =>
        {
            Assert.That(screen.Children.Select(part => part.Id),
                Is.EqualTo(new[] { "top", "the-demo", "what-to-watch" }));
            Assert.That(screen.NextElementSibling!.Id, Is.EqualTo("ask"), "the offer comes under the first screen");
            Assert.That(styles, Does.Contain("min-height: calc(100dvh - var(--stage-top) - var(--stage-foot));"));
            Assert.That(styles, Does.Contain(".stage-player { display: flex; flex: 1 0 auto; flex-direction: column;"));
        });
    }

    /// <summary>
    /// Only a player that can grow is given the screen's height. A business's window fills what it is given, so its
    /// stage says it fills. An agent's work window has a height of its own, and held to the screen's height its
    /// stage would leave a gap under it, so that stage says nothing and its offer follows its window.
    /// </summary>
    [Test]
    public void OnlyAStageWhosePlayerFills_IsHeldToTheScreensHeight()
    {
        using var ctx = new BunitContext();
        string styles = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/LouisAgent.Web/Components/Site/Showroom/Stage/DemoStage.razor.scss"));

        var filling = ctx.Render<DemoStage>(parameters => parameters
            .Add(stage => stage.Title, "Bread at dawn.")
            .Add(stage => stage.Player, "<div class=\"player\">The website</div>")
            .Add(stage => stage.Fills, true));

        Assert.Multiple(() =>
        {
            Assert.That(filling.Find(".demo-stage").ClassList, Does.Contain("is-filling"));
            Assert.That(Stage(ctx).Find(".demo-stage").ClassList, Does.Not.Contain("is-filling"));
            Assert.That(styles, Does.Contain(".demo-stage.is-filling .stage-screen {"));
        });
    }
}
