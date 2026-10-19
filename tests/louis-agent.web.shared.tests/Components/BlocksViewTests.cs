using LouisAgent.Web.Shared.Chat;
using LouisAgent.Web.Shared.Components;
using LouisAgent.Web.Shared.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Shared.Tests.Components;

// T1: the blocks of an answer (thinking, text, tool calls, a delegation card for a StartSubSession call), rendered
// from the cook's own turn inside the recorded one, so every kind of block is in the golden.
public class BlocksViewTests
{
    [Test]
    public async Task TheCooksBlocks_MatchTheGoldenFile()
    {
        var turn = await SessionStreamFixture.ReplayAsync();
        var cook = turn.Blocks.Single(b => b.ToolName == "StartSubSession").SubTurn!;
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<BlocksView>(p => p.Add(x => x.Blocks, cook.Blocks));

        Assert.That(cut.FindAll("details.tool"), Has.Count.EqualTo(cook.Blocks.Count(b => b.Kind == BlockKind.Tool)));
        Snapshots.Match("louis-agent.web.shared.tests", "Components/BlocksView.html", cut.Markup);
    }

    [Test]
    public void AThinkingBlock_IsOpenWhileItThinks_AndSaysHowLongAfter()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());
        var thinking = new Block { Kind = BlockKind.Thinking, Text = "Eers die winkel." };

        var running = ctx.Render<BlocksView>(p => p.Add(x => x.Blocks, [thinking]).Add(x => x.Streaming, true));
        Assert.That(running.Find("details.thinking").HasAttribute("open"), Is.True);
        Assert.That(running.Find("summary").TextContent, Is.EqualTo("Thinking…"));

        thinking.ThinkingSeconds = 3.4;
        var done = ctx.Render<BlocksView>(p => p.Add(x => x.Blocks, [thinking]));
        Assert.That(done.Find("details.thinking").HasAttribute("open"), Is.False);
        Assert.That(done.Find("summary").TextContent, Is.EqualTo("Thought for 3s"));
    }

    [Test]
    public void AFailedTool_IsMarkedFailed_WithItsResult()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());
        var call = new Block { Kind = BlockKind.Tool, ToolName = "FetchUrl", ToolInput = "{\n  \"url\": \"https://x\"\n}", ToolResult = "Error: 404", ToolFailed = true };

        var cut = ctx.Render<BlocksView>(p => p.Add(x => x.Blocks, [call]));

        Assert.That(cut.Find("details.tool").ClassList, Does.Contain("failed"));
        Assert.That(cut.Find(".tool-status").TextContent, Is.EqualTo("✗"));
        Assert.That(cut.Find(".tool-args").TextContent, Is.EqualTo("\"url\": \"https://x\""));
        Assert.That(cut.FindAll("pre").Select(p => p.TextContent), Does.Contain("Error: 404"));
    }
}
