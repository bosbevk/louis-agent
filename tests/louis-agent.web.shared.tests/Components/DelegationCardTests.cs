using LouisAgent.Web.Shared.Chat;
using LouisAgent.Web.Shared.Components;
using LouisAgent.Web.Shared.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Shared.Tests.Components;

// T1: the delegation card (F5): the cook's card from the recorded turn, done, pinned to DelegationCard.html; and the
// states a card goes through (running, done, failed) as the chat draws them.
public class DelegationCardTests
{
    private static (Block Block, Delegation Delegation) TheCooksCall(Turn turn)
    {
        var block = turn.Blocks.Single(b => b.ToolName == "StartSubSession");
        return (block, Delegation.From(block)!);
    }

    [Test]
    public async Task TheCooksCard_Done_MatchesTheGoldenFile()
    {
        var (block, delegation) = TheCooksCall(await SessionStreamFixture.ReplayAsync());
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<DelegationCard>(p => p.Add(x => x.Delegation, delegation).Add(x => x.Block, block));

        Assert.That(cut.Find("details.delegation").ClassList, Does.Contain("done"));
        Assert.That(cut.Find(".delegation-agent strong").TextContent, Is.EqualTo("puls-cook"), "the role, as the user thinks of it");
        Snapshots.Match("louis-agent.web.shared.tests", "Components/DelegationCard.html", cut.Markup);
    }

    [Test]
    public async Task ARunningCard_IsOpen_AndSaysItIsWorking()
    {
        var (block, _) = TheCooksCall(await SessionStreamFixture.ReplayAsync());
        block.ToolResult = null;
        var delegation = Delegation.From(block)!;
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<DelegationCard>(p => p.Add(x => x.Delegation, delegation).Add(x => x.Block, block).Add(x => x.Streaming, true));

        Assert.That(cut.Find("details.delegation").ClassList, Does.Contain("running"));
        Assert.That(cut.Find("details.delegation").HasAttribute("open"), Is.True);
        Assert.That(cut.Find(".delegation-status").TextContent, Is.EqualTo("Working"));
        Assert.That(cut.FindAll(".delegation-cost"), Is.Empty, "the cost comes with the rolled-up line");
    }

    [Test]
    public void AFailedCall_IsMarkedFailed_WithTheError()
    {
        var block = new Block
        {
            Kind = BlockKind.Tool, ToolUseId = "call_1", ToolName = "StartSubSession",
            ToolInput = """{"agent":"puls","toolsets":["web"],"message":"Kook","role":"puls-cook"}""",
            ToolResult = "Error: no peer named puls", ToolFailed = true,
        };
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<DelegationCard>(p => p.Add(x => x.Delegation, Delegation.From(block)!).Add(x => x.Block, block));

        Assert.That(cut.Find("details.delegation").ClassList, Does.Contain("failed"));
        Assert.That(cut.Find(".delegation-status").TextContent, Is.EqualTo("Failed"));
        Assert.That(cut.Find(".delegation-answer").TextContent, Does.Contain("Error: no peer named puls"));
    }
}
