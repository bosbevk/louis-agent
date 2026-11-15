using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget.Bar;
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
    /// The widget's script marks the place it lit, the module looks for that mark and says where the caption stands,
    /// and the styles read those names.
    /// </summary>
    [Test]
    public void TheScripts_AndTheStyles_UseTheSameNames()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WidgetScript, Does.Contain("dataset.lit"), "the widget's script marks the place it lit");
            Assert.That(Module, Does.Contain("[data-lit]"), "the module finds the place by that mark");
            Assert.That(Module, Does.Contain("--caption-x").And.Contain("--caption-y").And.Contain("\"is-beside\""));
            Assert.That(WideBlock, Does.Contain(".agent-caption.is-beside"));
            Assert.That(WideBlock, Does.Contain("var(--caption-x)").And.Contain("var(--caption-y)"));
        });
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
            Assert.That(outside, Does.Not.Contain("is-beside"));
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

    /// <summary>The bar over a runner whose last turn showed a place and said a line about it.</summary>
    private static IRenderedComponent<AgentBar> Bar(BunitContext ctx)
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
        var turn = new Turn { UserText = "Where?", Blocks = [shown, said] };
        var chat = new LouisAgent.Web.Chats.Chat { SessionId = "sess_fixture", Turns = [turn] };
        var runner = new ChatRunner(api, new ChatStore(ctx.JSInterop.JSRuntime, settings), new ChatText(),
            () => Task.FromResult("sess_fixture"))
        { Chat = chat };
        return ctx.Render<AgentBar>(parameters => parameters.Add(bar => bar.Runner, runner));
    }
}
