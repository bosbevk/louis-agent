using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget.Bar;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Bar.Caption;

/// <summary>
/// F134.5: the guide's caption turns over to show how its answer was made: the tools it called, how long it thought,
/// and its usage, the numbers the API priced from the ledger. The front carries the answer's cost.
/// </summary>
public class CaptionTurnedOverTests
{
    /// <summary>What the API measured of the answer: four requests, priced in dollars.</summary>
    private static readonly TurnUsage Measured = new(6, 1400, 37000, 1200, 4, 0.0042m, "USD", 0);

    /// <summary>
    /// The bar over a runner whose chat holds one answer that thought, showed a place and said a line.
    /// </summary>
    private static IRenderedComponent<AgentBar> Bar(BunitContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
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
        var thought = new Block { Kind = BlockKind.Thinking, Text = "…", ThinkingSeconds = 1.25 };
        var said = new Block { Kind = BlockKind.Text, Text = "These are the levels." };
        var turn = new Turn { UserText = "Where?", Blocks = [thought, shown, said], Usage = Measured };
        var chat = new LouisAgent.Web.Chats.Chat { SessionId = "sess_fixture", Turns = [turn] };
        var runner = new ChatRunner(api, new ChatStore(ctx.JSInterop.JSRuntime, settings), new ChatText(),
            () => Task.FromResult("sess_fixture"))
        { Chat = chat };
        return ctx.Render<AgentBar>(parameters => parameters
            .Add(bar => bar.Runner, runner)
            .Add(bar => bar.Title, "Louis · AI"));
    }

    /// <summary>The front carries what the answer cost, beside the guide's name.</summary>
    [Test]
    public void TheFront_CarriesTheAnswersCost()
    {
        using var ctx = new BunitContext();

        var cut = Bar(ctx);

        Assert.That(cut.Find(".agent-caption .caption-cost").TextContent, Is.EqualTo(UsageFormat.Cost(Measured)));
    }

    /// <summary>
    /// Turned over, the caption shows the tools the answer called, how long it thought and its usage line, with the
    /// exact figures in the line's tooltip; the answer's words wait on the front.
    /// </summary>
    [Test]
    public void TurnedOver_TheCaptionShowsHowTheAnswerWasMade()
    {
        using var ctx = new BunitContext();
        var cut = Bar(ctx);

        cut.Find(".agent-caption .caption-turn").Click();

        var usage = cut.Find(".agent-caption .caption-usage");
        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".caption-tools li").Select(tool => tool.TextContent.Trim()),
                Is.EqualTo(new[] { "ShowPage /identities/levels" }));
            Assert.That(cut.Find(".caption-thought").TextContent, Is.EqualTo("Thought for 1.3s"));
            Assert.That(usage.TextContent, Is.EqualTo(UsageFormat.Line(Measured)));
            Assert.That(usage.GetAttribute("title"), Is.EqualTo(UsageFormat.Detail(Measured)));
            Assert.That(cut.FindAll(".caption-says"), Is.Empty);
        });
    }

    /// <summary>Turned back, the answer's words are there again.</summary>
    [Test]
    public void TurnedBack_TheAnswerIsThereAgain()
    {
        using var ctx = new BunitContext();
        var cut = Bar(ctx);

        cut.Find(".agent-caption .caption-turn").Click();
        cut.Find(".agent-caption .caption-turn").Click();

        Assert.That((cut.FindAll(".caption-says").Count, cut.FindAll(".caption-tools").Count), Is.EqualTo((1, 0)));
    }
}
