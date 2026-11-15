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
    /// Turned over, the caption shows the tools the answer called, then three figures as the design drew them: how
    /// long it thought, the tokens it wrote with what was read from the cache, and what the answer cost. The exact
    /// figures are in their tooltip.
    /// </summary>
    [Test]
    public void TheBack_ShowsHowTheAnswerWasMade()
    {
        using var ctx = new BunitContext();

        var back = Bar(ctx).Find(".agent-caption .caption-back");

        var figures = back.QuerySelectorAll(".caption-sum .caption-figure")
            .Select(figure => (figure.QuerySelector("b")!.TextContent, figure.QuerySelector("span")!.TextContent));
        Assert.Multiple(() =>
        {
            Assert.That(back.QuerySelector(".caption-who b")!.TextContent, Is.EqualTo("How I did that"));
            Assert.That(back.QuerySelector(".caption-note")!.TextContent, Is.EqualTo("measured by the framework"));
            Assert.That(back.QuerySelectorAll(".caption-tools li").Select(tool => tool.TextContent.Trim()),
                Is.EqualTo(new[] { "ShowPage /identities/levels" }));
            Assert.That(figures, Is.EqualTo(new[]
            {
                ("1.3s", "thinking"), ("1.2k", "tokens out, 37k cached"), ("$0.0042", "this answer")
            }));
            Assert.That(back.QuerySelector(".caption-sum")!.GetAttribute("title"),
                Is.EqualTo(UsageFormat.Detail(Measured)));
        });
    }

    /// <summary>
    /// The caption is one card with two sides, as the design drew it, so it can turn. The side turned away takes no
    /// press and is not read aloud: the back at first, and the front once it is turned over.
    /// </summary>
    [Test]
    public void TurnedOver_TheBackFaces_AndTheFrontIsOutOfReach()
    {
        using var ctx = new BunitContext();
        var cut = Bar(ctx);
        var before = (Turned(cut), Inert(cut, ".caption-front"), Inert(cut, ".caption-back"));

        cut.Find(".caption-front .caption-turn").Click();

        var after = (Turned(cut), Inert(cut, ".caption-front"), Inert(cut, ".caption-back"));
        Assert.That((before, after), Is.EqualTo(((false, false, true), (true, true, false))));
    }

    /// <summary>Turned back, the answer faces again.</summary>
    [Test]
    public void TurnedBack_TheAnswerFacesAgain()
    {
        using var ctx = new BunitContext();
        var cut = Bar(ctx);

        cut.Find(".caption-front .caption-turn").Click();
        cut.Find(".caption-back .caption-turn").Click();

        Assert.That((Turned(cut), Inert(cut, ".caption-front"), Inert(cut, ".caption-back")),
            Is.EqualTo((false, false, true)));
    }

    /// <summary>Whether the caption is turned over.</summary>
    private static bool Turned(IRenderedComponent<AgentBar> cut) =>
        cut.Find(".agent-caption").ClassList.Contains("is-turned");

    /// <summary>Whether a side of the caption is out of reach.</summary>
    private static bool Inert(IRenderedComponent<AgentBar> cut, string side) => cut.Find(side).HasAttribute("inert");
}
