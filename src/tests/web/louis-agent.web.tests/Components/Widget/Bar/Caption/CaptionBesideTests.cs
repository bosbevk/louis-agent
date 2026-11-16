using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget.Bar;
using LouisAgent.Web.Components.Widget.Bar.Caption;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Bar.Caption;

/// <summary>
/// F134.4: on a wide screen the agent's caption stands beside the place the agent lit. The widget's script marks the
/// place, the caption's module stands the caption beside it, and its styles draw it there, so the three must use the
/// same names; on a phone nothing is moved.
/// </summary>
public class CaptionBesideTests
{
    private static readonly string Web = Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web/Components/Widget");

    /// <summary>A file of the widget's, its line ends folded.</summary>
    private static string Read(string path) => File.ReadAllText(Path.Combine(Web, path)).ReplaceLineEndings("\n");

    private static string Module => Read("Bar/Caption/AgentCaption.razor.js");
    private static string Styles => Read("Bar/Caption/AgentCaption.razor.scss");
    private static string WidgetScript => Read("ChatWidget.razor.js");

    /// <summary>The screens the module counts as wide, as it says them to the browser.</summary>
    private static string WideQuery
    {
        get
        {
            const string opening = "const wide = \"";
            int start = Module.IndexOf(opening, StringComparison.Ordinal);
            return start < 0 ? "" : Module[(start + opening.Length)..Module.IndexOf('"', start + opening.Length)];
        }
    }

    /// <summary>The styles' block for those screens, up to its closing brace; empty when there is none.</summary>
    private static string WideBlock
    {
        get
        {
            int start = Styles.IndexOf($"@media {WideQuery} {{", StringComparison.Ordinal);
            if (WideQuery.Length == 0 || start < 0) return "";
            return Styles[start..Styles.IndexOf("\n}", start, StringComparison.Ordinal)];
        }
    }

    /// <summary>
    /// The widget's script marks the place it lit and says so, the module hears it, looks for that mark and says where
    /// the caption stands, and the styles read those names. The caption is fixed to the screen there, so it stands
    /// where the module says whatever the bar under it does.
    /// </summary>
    [Test]
    public void TheScripts_AndTheStyles_UseTheSameNames()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WidgetScript, Does.Contain("dataset.lit"), "the widget's script marks the place it lit");
            Assert.That(WidgetScript, Does.Contain("\"place-lit\""), "and says that it did");
            Assert.That(Module, Does.Contain("[data-lit]"), "the module finds the place by that mark");
            Assert.That(Module, Does.Contain("\"place-lit\""), "and hears when another place is lit");
            Assert.That(Module, Does.Contain("--caption-x").And.Contain("--caption-y").And.Contain("dataset.stands"));
            Assert.That(WideBlock, Does.Contain(".agent-caption[data-stands]"));
            Assert.That(WideBlock, Does.Contain("position: fixed;"));
            Assert.That(WideBlock, Does.Contain("var(--caption-x)").And.Contain("var(--caption-y)"));
        });
    }

    /// <summary>
    /// F151.4, FX163: when the piece the answer is about stands in a row of cards, the agent's words take the place of
    /// the cards beside it, as the row's rule says (Louis's two pictures): in a row of four, the two on the far side;
    /// in a row of three or fewer, the next one, or the one before for the last. Where they stand, how wide and tall
    /// they are together, and their corners. The module says so by a mark of its own with those measures, and the
    /// styles read them. With no row the words stand beside the piece as before, which the other tests here hold.
    /// </summary>
    [Test]
    public void InARowOfCards_TheWordsTakeTheirNeighboursPlace_TheirSizeAndTheirCorners()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Module, Does.Contain("const ROW_GIVES = { 4: [[1, 2], [2, 3], [0, 1], [1, 2]] };"));
            Assert.That(Module, Does.Contain("(at + 1 < count ? [at + 1] : [at - 1])"),
                "one beside, in a row of three");
            Assert.That(Module, Does.Contain("const WIDE_SHARE = 1 / 3;"), "a wide piece alone gives its right third");
            Assert.That(Module, Does.Contain("matches(\"[data-piece]\")"), "a card is a piece of the page");
            Assert.That(Module, Does.Contain("dataset.inPlace"), "the module's mark, an attribute of its own");
            Assert.That(Module, Does.Contain("--caption-w").And.Contain("--caption-h").And.Contain("--caption-r"));
            Assert.That(WideBlock,
                Does.Contain(".agent-caption[data-stands][data-in-place] { width: var(--caption-w); }"));
            Assert.That(WideBlock, Does.Contain("min-height: var(--caption-h);"));
            Assert.That(WideBlock, Does.Contain("border-radius: var(--caption-r);"));
        });
    }

    /// <summary>
    /// A visitor who pressed a piece of the page is asking about that piece. When the answer lights no place of its
    /// own, the caption stands beside the piece they chose, where the answer belongs, and not beside the bar.
    /// </summary>
    [Test]
    public void WithNoPlaceLit_TheCaptionStandsBesideThePieceTheVisitorChose()
    {
        string pieces = Read("Pieces/PieceWatch.razor.js");

        Assert.Multiple(() =>
        {
            Assert.That(Module, Does.Contain("?? document.querySelector(\"[data-piece][data-selected]\")"));
            Assert.That(pieces, Does.Contain("setAttribute(\"data-selected\""),
                "the piece watch marks the chosen piece by that name");
        });
    }

    /// <summary>
    /// The module's mark on the caption is an attribute of its own and never a class. The page writes the caption's
    /// classes again whenever they change, as when it turns over, and that would wipe a class the module had added
    /// and drop the caption back over the bar.
    /// </summary>
    [Test]
    public void TheModulesMark_IsNotAClass_SoTurningTheCaptionKeepsItInPlace()
    {
        Assert.That(Module, Does.Not.Contain("caption.classList"));
    }

    /// <summary>
    /// A place the agent lights may still be moving, as a window coming forward on a deck is. The caption follows it
    /// for a moment after it is lit, frame by frame, and then rests.
    /// </summary>
    [Test]
    public void AfterAPlaceIsLit_TheCaptionFollowsItForAMoment()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Module, Does.Contain("document.addEventListener(\"place-lit\", stood.settle)"));
            Assert.That(Module, Does.Contain("requestAnimationFrame"));
        });
    }

    /// <summary>
    /// While the caption stands beside a place, the place keeps a quiet ring, as the design's light stays on what the
    /// guide is talking about. The module lays the ring and the web's shared styles draw it, by one name.
    /// </summary>
    [Test]
    public void ThePlaceKeepsARing_WhileTheCaptionStandsBesideIt()
    {
        string shared = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Styles/web/base/_elements.scss"));

        Assert.Multiple(() =>
        {
            Assert.That(Module, Does.Contain("\"caption-ring\""));
            Assert.That(Module, Does.Contain("ring.remove()"), "the ring goes with the caption");
            Assert.That(shared, Does.Contain(".caption-ring {").And.Contain(".caption-ring.is-on"));
        });
    }

    /// <summary>
    /// A caption with no place to stand beside (its answer showed none, or the place is as wide as the screen) stands
    /// beside the bar where the screen has room, so the page under it stays as it is. Only on a narrower screen does
    /// it stand over the bar. The place keeps its ring only when the caption stands beside the place itself.
    /// </summary>
    [Test]
    public void WithNoPlaceToStandBeside_TheCaptionStandsBesideTheBar()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Module, Does.Contain("spotFor(box, width, height, floor)"));
            Assert.That(Module, Does.Contain("?? besideTheBar(row, width, height)"), "the bar's side comes second");
            Assert.That(Module, Does.Contain("x: bar.left - gap - width"),
                "at the bar's left, beside the orb, clear of a demo's live window at the page's right");
            Assert.That(Module, Does.Contain("ring.classList.toggle(\"is-on\", beside !== null)"));
            Assert.That(Module, Does.Contain("delete caption.dataset.stands"), "with no room, it is left over the bar");
        });
    }

    /// <summary>
    /// Only an answer that showed a place stands beside one: the mark of an earlier answer's place stays on the page,
    /// and an answer about nothing on the page must not stand beside it.
    /// </summary>
    [Test]
    public void OnlyAnAnswerThatShowedAPlace_IsStoodBesideOne()
    {
        using var showing = new BunitContext();
        using var saying = new BunitContext();
        showing.JSInterop.Mode = JSRuntimeMode.Loose;
        saying.JSInterop.Mode = JSRuntimeMode.Loose;

        var shownAndSaid = Bar(showing).Find(".agent-caption").HasAttribute("data-beside");
        var saidOnly = Bar(saying, shows: false).Find(".agent-caption").HasAttribute("data-beside");

        Assert.Multiple(() =>
        {
            Assert.That((shownAndSaid, saidOnly), Is.EqualTo((true, false)));
            Assert.That(Module, Does.Contain("\"data-beside\""), "the module reads the same mark");
        });
    }

    /// <summary>
    /// An answer's words can come before it shows its place. When the place comes, the caption asks its module to
    /// stand it again.
    /// </summary>
    [Test]
    public void WhenThePlaceComesAfterTheWords_TheCaptionIsStoodAgain()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.Caption);
        var work = new AnswerWork([], 0, null);
        var cut = ctx.Render<AgentCaption>(parameters => parameters
            .Add(caption => caption.Title, "Louis · AI")
            .Add(caption => caption.Words, "Here.")
            .Add(caption => caption.Work, work));

        cut.Render(parameters => parameters.Add(caption => caption.Beside, true));

        Assert.That(module.Invocations.Select(call => call.Identifier),
            Is.EqualTo(new[] { "standBeside", "standAgain" }));
    }

    /// <summary>
    /// On a phone nothing stands beside: the module asks the browser first, and only the wide styles move it.
    /// </summary>
    [Test]
    public void OnAPhone_TheCaptionStaysOverTheBar()
    {
        string outside = Styles.Replace(WideBlock, "", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(WideQuery, Is.EqualTo("(min-width: 901px)"));
            Assert.That(Module, Does.Contain("if (!matchMedia(wide).matches) return;"));
            Assert.That(outside, Does.Not.Contain("data-stands"));
        });
    }

    /// <summary>
    /// Once drawn the caption asks its module to stand it beside the lit place, and when it goes, to let it go.
    /// </summary>
    [Test]
    public void TheCaption_StandsBesideOnceDrawn_AndLetsGoWhenItGoes()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.Caption);
        var cut = Bar(ctx);

        cut.Find(".agent-caption .caption-talk").Click();

        Assert.That(module.Invocations.Select(call => call.Identifier), Is.EqualTo(new[] { "standBeside", "letGo" }));
    }

    /// <summary>The bar over a runner whose last turn said a line, about a place it showed unless told not.</summary>
    private static IRenderedComponent<AgentBar> Bar(BunitContext ctx, bool shows = true)
    {
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out _);
        ctx.Services.AddSingleton(new ChatText());
        var shown = new Block
        {
            Kind = BlockKind.Tool,
            ToolName = "ShowPage",
            ToolUseId = "toolu_show",
            ToolInput = """{"path":"/identities/levels"}""",
            ToolResult = "Shown."
        };
        var said = new Block { Kind = BlockKind.Text, Text = "Here." };
        var turn = new Turn { UserText = "Where?", Blocks = shows ? [shown, said] : [said] };
        var chat = new LouisAgent.Web.Chats.Chat { SessionId = "sess_fixture", Turns = [turn] };
        var runner = new ChatRunner(api, new ChatStore(ctx.JSInterop.JSRuntime, settings), new ChatText(),
            () => Task.FromResult("sess_fixture"))
        { Chat = chat };
        return ctx.Render<AgentBar>(parameters => parameters.Add(bar => bar.Runner, runner));
    }
}
