using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget.Bar;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Bar;

/// <summary>
/// F134.5: the bar carries what the talk has cost so far and how many answers it holds, and a press opens the whole
/// talk, whose foot says the same total, as the chat's usage line says it.
/// </summary>
public class AgentTotalTests
{
    private static readonly TurnUsage First = new(6, 1400, 37000, 1200, 4, 0.004m, "USD", 0);
    private static readonly TurnUsage Second = new(4, 0, 38000, 600, 2, 0.002m, "USD", 0);

    /// <summary>The bar over a runner whose chat holds these answers, each measured or not.</summary>
    private static IRenderedComponent<AgentBar> Bar(BunitContext ctx, params TurnUsage?[] usages)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out _);
        ctx.Services.AddSingleton(new ChatText());
        var turns = usages.Select(usage => new Turn
        {
            UserText = "Hi",
            Blocks = [new Block { Kind = BlockKind.Text, Text = "Hello." }],
            Usage = usage
        }).ToList();
        var chat = new LouisAgent.Web.Chats.Chat { SessionId = "sess_fixture", Turns = turns };
        var runner = new ChatRunner(api, new ChatStore(ctx.JSInterop.JSRuntime, settings), new ChatText(),
            () => Task.FromResult("sess_fixture"))
        { Chat = chat };
        return ctx.Render<AgentBar>(parameters => parameters.Add(bar => bar.Runner, runner));
    }

    /// <summary>
    /// The bar's total is the talk's cost so far over its count of answers, the exact line in its title. It stands
    /// in the box's end, before the send button, as the design drew it.
    /// </summary>
    [Test]
    public void TheTotal_IsTheTalksCost_OverItsAnswers()
    {
        using var ctx = new BunitContext();

        var total = Bar(ctx, First, Second).Find(".agent-box .composer-tools .agent-total");

        Assert.Multiple(() =>
        {
            Assert.That((total.QuerySelector("b")!.TextContent, total.QuerySelector("span")!.TextContent),
                Is.EqualTo(("$0.006", "2 answers")));
            Assert.That(total.GetAttribute("title"), Is.EqualTo(UsageFormat.Total(First + Second)));
            Assert.That(total.NextElementSibling?.ClassList, Does.Contain("send"));
        });
    }

    /// <summary>A press opens the whole talk, and its foot says the talk's total.</summary>
    [Test]
    public void TheTotal_OpensTheWholeTalk_WithTheTotalAtItsFoot()
    {
        using var ctx = new BunitContext();
        var cut = Bar(ctx, First, Second);

        cut.Find(".agent-total").Click();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".agent-talk"), Has.Count.EqualTo(1));
            Assert.That(cut.Find(".agent-talk-total").TextContent, Is.EqualTo(UsageFormat.Total(First + Second)));
        });
    }

    /// <summary>A talk with nothing measured yet shows no total, rather than a cost of nothing.</summary>
    [Test]
    public void ATalkWithNothingMeasured_ShowsNoTotal()
    {
        using var ctx = new BunitContext();

        var cut = Bar(ctx, (TurnUsage?)null);

        Assert.That(cut.FindAll(".agent-total"), Is.Empty);
    }
}
