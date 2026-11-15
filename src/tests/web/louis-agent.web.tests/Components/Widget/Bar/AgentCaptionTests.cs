using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget.Bar;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Bar;

/// <summary>
/// F134.4: when the guide's last answer showed a place, its words stand as a caption over the bar, the guide's name
/// on it and a way into the box under them, where the folded talk stood.
/// </summary>
public class AgentCaptionTests
{
    /// <summary>A call to show the levels on Identities, as the guide makes it.</summary>
    private static Block ShowLevels() => new()
    {
        Kind = BlockKind.Tool,
        ToolName = "ShowPage",
        ToolUseId = "toolu_show",
        ToolInput = """{"path":"/identities/levels"}""",
        ToolResult = "Shown."
    };

    /// <summary>A call offering two demos, the restaurant suggested, as the guide makes it.</summary>
    private static Block OfferDemos() => new()
    {
        Kind = BlockKind.Tool,
        ToolName = "Offer",
        ToolUseId = "toolu_offer",
        ToolInput = """
                    {"question":"Which one?","choices":[{"label":"The restaurant","why":"Bookings","suggested":true},
                    {"label":"The law firm"}]}
                    """,
        ToolResult = "Offered."
    };

    /// <summary>The bar over a runner whose chat holds this one turn, done.</summary>
    private static IRenderedComponent<AgentBar> Bar(BunitContext ctx, Turn turn, Action<string>? chosen = null)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out _);
        ctx.Services.AddSingleton(new ChatText());
        var chat = new LouisAgent.Web.Chats.Chat { SessionId = "sess_fixture", Turns = [turn] };
        var runner = new ChatRunner(api, new ChatStore(ctx.JSInterop.JSRuntime, settings), new ChatText(),
            () => Task.FromResult("sess_fixture"))
        { Chat = chat };
        return ctx.Render<AgentBar>(parameters => parameters
            .Add(bar => bar.Runner, runner)
            .Add(bar => bar.Title, "Louis · AI")
            .Add(bar => bar.OnChoose, label => chosen?.Invoke(label)));
    }

    /// <summary>A turn that showed the levels and said a line about them.</summary>
    private static Turn ShowedAndSaid() => new()
    {
        UserText = "Where are the levels?",
        Blocks = [ShowLevels(), new Block { Kind = BlockKind.Text, Text = "These are the levels." }]
    };

    /// <summary>
    /// The last answer showed a place, so its words stand as the caption, with the guide's name and a way into the
    /// box; the folded talk gives way to it.
    /// </summary>
    [Test]
    public void AfterShowingAPlace_TheAnswerIsACaption()
    {
        using var ctx = new BunitContext();

        var cut = Bar(ctx, ShowedAndSaid());

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".caption-front .caption-who b").TextContent, Is.EqualTo("Louis · AI"));
            Assert.That(cut.FindAll(".caption-front .caption-who .caption-orb"), Has.Count.EqualTo(1));
            Assert.That(cut.Find(".agent-caption .caption-says").TextContent.Trim(),
                Is.EqualTo("These are the levels."));
            Assert.That(cut.Find(".agent-caption label.caption-ask").GetAttribute("for"), Is.EqualTo("composer-input"));
            Assert.That(cut.FindAll(".agent-talk"), Is.Empty);
        });
    }

    /// <summary>
    /// An answer that showed no place stands as a caption too, as the design has every answer: over the bar, since
    /// there is no place to stand beside.
    /// </summary>
    [Test]
    public void AnAnswerThatShowedNoPlace_StandsAsACaptionToo()
    {
        using var ctx = new BunitContext();
        var turn = new Turn { UserText = "Hi", Blocks = [new Block { Kind = BlockKind.Text, Text = "Hello." }] };

        var cut = Bar(ctx, turn);

        Assert.That((cut.FindAll(".agent-caption").Count, cut.FindAll(".agent-talk").Count), Is.EqualTo((1, 0)));
    }

    /// <summary>
    /// An answer that asks with a form is one a caption can't hold, so the whole talk opens with it, where the form
    /// is drawn; folding the talk puts that answer away.
    /// </summary>
    [Test]
    public void AnAnswerWithAForm_OpensTheWholeTalk_AndFoldingPutsItAway()
    {
        using var ctx = new BunitContext();
        var form = new Block
        {
            Kind = BlockKind.Tool,
            ToolName = "AskFor",
            ToolUseId = "toolu_ask",
            ToolInput = """{"question":"How can Louis reach you?","fields":[{"label":"Your email","kind":"email"}]}""",
            ToolResult = "Asked."
        };
        var said = new Block { Kind = BlockKind.Text, Text = "Leave your details." };
        var cut = Bar(ctx, new Turn { UserText = "Ask Louis for me", Blocks = [said, form] });
        var opened = (cut.FindAll(".agent-caption").Count, cut.FindAll(".agent-talk form").Count);

        cut.Find(".agent-talk-toggle").Click();

        var folded = (cut.FindAll(".agent-caption").Count, cut.FindAll(".agent-talk").Count);
        Assert.That((opened, folded), Is.EqualTo(((0, 1), (0, 0))));
    }

    /// <summary>The caption's button opens the whole talk in its place, and folding the talk brings it back.</summary>
    [Test]
    public void TheCaptionsTalkButton_OpensTheWholeTalk_AndFoldingBringsTheCaptionBack()
    {
        using var ctx = new BunitContext();
        var cut = Bar(ctx, ShowedAndSaid());

        cut.Find(".agent-caption .caption-talk").Click();
        var opened = (cut.FindAll(".agent-caption").Count, cut.FindAll(".agent-talk").Count);
        cut.Find(".agent-talk-toggle").Click();

        var folded = (cut.FindAll(".agent-caption").Count, cut.FindAll(".agent-talk").Count);
        Assert.That((opened, folded), Is.EqualTo(((0, 1), (1, 0))));
    }

    /// <summary>
    /// The caption's close puts the answer away: the caption goes, the site's chips come back, and the next answer
    /// stands as a caption again.
    /// </summary>
    [Test]
    public void PutAway_TheCaptionGoes_UntilTheNextAnswer()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new AgentSuggestions(["Walk me through the demos"]));
        var cut = Bar(ctx, ShowedAndSaid());

        cut.Find(".agent-caption .caption-close").Click();
        var away = (cut.FindAll(".agent-caption").Count, cut.FindAll(ChipsShown).Count);
        cut.Instance.Runner.Chat!.Turns.Add(ShowedAndSaid());
        cut.Render();

        Assert.That((away, cut.FindAll(".agent-caption").Count), Is.EqualTo(((0, 1), 1)));
    }

    /// <summary>
    /// The choices the guide offered in that answer stand in the caption as pills, as the design drew them: the
    /// question over them, the suggested one marked, a choice's why as its tip, and a press asks with its label, as
    /// a card in the talk does.
    /// </summary>
    [Test]
    public void TheCaptionsChoices_ArePills_ThatAskWithTheirLabel()
    {
        using var ctx = new BunitContext();
        string? asked = null;
        var turn = new Turn
        {
            UserText = "Show me a demo",
            Blocks = [ShowLevels(), new Block { Kind = BlockKind.Text, Text = "Here they are." }, OfferDemos()]
        };
        var cut = Bar(ctx, turn, label => asked = label);

        var suggested = cut.Find(".agent-caption button.caption-choice.is-suggested");
        suggested.Click();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".agent-caption .caption-question").TextContent, Is.EqualTo("Which one?"));
            Assert.That(cut.FindAll(".agent-caption .caption-choice").Select(choice => choice.TextContent.Trim()),
                Is.EqualTo(new[] { "The restaurant", "The law firm" }));
            Assert.That(suggested.GetAttribute("title"), Is.EqualTo("Bookings"));
            Assert.That(asked, Is.EqualTo("The restaurant"));
        });
    }

    /// <summary>
    /// A choice that is the visitor's own words is a pill that puts them in the guide's box, where they write it: a
    /// label for the box, so no script is needed.
    /// </summary>
    [Test]
    public void AChoiceToWrite_PutsTheVisitorInTheBox()
    {
        using var ctx = new BunitContext();
        var offer = new Block
        {
            Kind = BlockKind.Tool,
            ToolName = "Offer",
            ToolUseId = "toolu_offer",
            ToolInput = """
                        {"question":"What is your business?","choices":[{"label":"A guesthouse"},
                        {"label":"Something else","writes":"text"}]}
                        """,
            ToolResult = "Offered."
        };
        var turn = new Turn { UserText = "Which demo is like mine?", Blocks = [ShowLevels(), offer] };

        var cut = Bar(ctx, turn);

        var writes = cut.Find(".agent-caption label.caption-choice");
        Assert.That((writes.TextContent.Trim(), writes.GetAttribute("for")),
            Is.EqualTo(("Something else", "composer-input")));
    }

    /// <summary>
    /// An answer that showed a place and only offered choices, with no words of its own, still stands as a caption.
    /// What it asked over its choices is then all it said, sometimes a whole explanation, so it is drawn as the
    /// answer's words and not a second time as the quiet question.
    /// </summary>
    [Test]
    public void AnAnswerOfOnlyChoices_SaysItsQuestionAsItsWords()
    {
        using var ctx = new BunitContext();
        var turn = new Turn { UserText = "Show me a demo", Blocks = [ShowLevels(), OfferDemos()] };

        var cut = Bar(ctx, turn);

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".agent-caption .caption-choice"), Has.Count.EqualTo(2));
            Assert.That(cut.Find(".agent-caption .caption-says").TextContent.Trim(), Is.EqualTo("Which one?"));
            Assert.That(cut.FindAll(".agent-caption .caption-question"), Is.Empty);
        });
    }

    /// <summary>
    /// When the chips come back after an answer, they follow the talk: the site's first question, then the choices
    /// the guide last offered, its suggested one bright.
    /// </summary>
    [Test]
    public void AfterAnAnswerIsPutAway_TheChipsAreItsChoices()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new AgentSuggestions(["Walk me through this page", "What does it cost?"]));
        var said = new Block { Kind = BlockKind.Text, Text = "Here they are." };
        var cut = Bar(ctx, new Turn { UserText = "Show me a demo", Blocks = [ShowLevels(), said, OfferDemos()] });

        cut.Find(".agent-caption .caption-close").Click();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll($"{ChipsShown} button").Select(chip => chip.TextContent),
                Is.EqualTo(new[] { "Walk me through this page", "The restaurant", "The law firm" }));
            Assert.That(cut.Find($"{ChipsShown} button.is-suggested").TextContent, Is.EqualTo("The restaurant"));
        });
    }

    /// <summary>The chips while they show: not stepped away.</summary>
    private const string ChipsShown = ".agent-chips:not(.is-away)";

    /// <summary>
    /// While a caption stands, the site's suggestion chips give way to it: the caption holds the guide's own choices
    /// and the way into the box, so a second row of shortcuts over the page would only crowd it. They stay drawn,
    /// stepped away and out of reach, so they can ease out and in and never pop.
    /// </summary>
    [Test]
    public void WhileACaptionStands_TheChipsStepAway()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new AgentSuggestions(["Walk me through the demos"]));

        var cut = Bar(ctx, ShowedAndSaid());

        Assert.Multiple(() =>
        {
            Assert.That((cut.FindAll(".agent-caption").Count, cut.FindAll(ChipsShown).Count), Is.EqualTo((1, 0)));
            Assert.That(cut.Find(".agent-chips.is-away").HasAttribute("inert"), "away, the chips take no press");
        });
    }

    /// <summary>
    /// The chips give way to the whole talk too, once it is open: on a phone it is a sheet most of the screen tall, and
    /// the chips over it would stand on the page's header.
    /// </summary>
    [Test]
    public void WhileTheWholeTalkIsOpen_TheChipsGiveWay()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new AgentSuggestions(["Walk me through the demos"]));
        var cut = Bar(ctx, ShowedAndSaid());

        cut.Find(".agent-caption .caption-talk").Click();

        Assert.That((cut.FindAll(".agent-talk").Count, cut.FindAll(ChipsShown).Count), Is.EqualTo((1, 0)));
    }
}
