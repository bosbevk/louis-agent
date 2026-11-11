using LouisAgent.Web.Chats;
using LouisAgent.Web.Components;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components;

/// <summary>
/// The blocks of an answer (thinking, text, tool calls, a delegation card for an AskAgent call), rendered from
/// the cook's own turn inside the recorded one, so every kind of block is in the golden.
/// </summary>
public class BlocksViewTests
{
    /// <summary>The cook's blocks match the golden file.</summary>
    [Test]
    public async Task TheCooksBlocks_MatchTheGoldenFile()
    {
        var turn = await SessionStreamFixture.ReplayAsync();
        var cook = turn.Blocks.Single(b => b.ToolName == Delegation.AskToolName).SubTurn!;
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<BlocksView>(p => p.Add(x => x.Blocks, cook.Blocks));

        Assert.That(cut.FindAll("details.tool"), Has.Count.EqualTo(cook.Blocks.Count(b => b.Kind == BlockKind.Tool)));
        Snapshots.Match("Components/BlocksView.html", cut.Markup);
    }

    /// <summary>
    /// With its work hidden, an answer is its words and a thinking label (F128.1, Louis: "do not show the tool calls in
    /// the agent, not its thinking, hide it", then "it must just show the label"): no tool call and no thinking text
    /// is drawn, so none is in the page to read either.
    /// </summary>
    [Test]
    public async Task WithItsWorkHidden_AnAnswerIsItsWordsAndAThinkingLabel()
    {
        var turn = await SessionStreamFixture.ReplayAsync();
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<BlocksView>(p => p.Add(x => x.Blocks, turn.Blocks).Add(x => x.ShowWork, false));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".tool, .delegation, .thinking-text, details"), Is.Empty);
            Assert.That(cut.FindAll(".thinking-label"),
                Has.Count.EqualTo(turn.Blocks.Count(b => b.Kind == BlockKind.Thinking)));
            Assert.That(cut.FindAll(".markdown"), Has.Count.EqualTo(turn.Blocks.Count(b => b.Kind == BlockKind.Text)));
        });
    }

    /// <summary>While it thinks with its work hidden, the label says so, and after, how long it thought.</summary>
    [Test]
    public void AHiddenThinkingBlock_ShowsItsLabelAlone()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());
        var thinking = new Block
        {
            Kind = BlockKind.Thinking,
            Text = "Eers die winkel."
        };

        var running = ctx.Render<BlocksView>(p => p.Add(x => x.Blocks, [thinking]).Add(x => x.ShowWork, false)
            .Add(x => x.Streaming, true));
        thinking.ThinkingSeconds = 3.4;
        var done = ctx.Render<BlocksView>(p => p.Add(x => x.Blocks, [thinking]).Add(x => x.ShowWork, false));

        Assert.Multiple(() =>
        {
            Assert.That(running.Find(".thinking-label").TextContent, Is.EqualTo("Thinking…"));
            Assert.That(done.Find(".thinking-label").TextContent, Is.EqualTo("Thought for 3s"));
            Assert.That(done.Markup, Does.Not.Contain("Eers die winkel."));
        });
    }

    /// <summary>A thinking block is open while it thinks, and says how long after.</summary>
    [Test]
    public void AThinkingBlock_IsOpenWhileItThinks_AndSaysHowLongAfter()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());
        var thinking = new Block
        {
            Kind = BlockKind.Thinking,
            Text = "Eers die winkel."
        };

        var running = ctx.Render<BlocksView>(p => p.Add(x => x.Blocks, [thinking]).Add(x => x.Streaming, true));
        Assert.That(running.Find("details.thinking").HasAttribute("open"), Is.True);
        Assert.That(running.Find("summary").TextContent, Is.EqualTo("Thinking…"));

        thinking.ThinkingSeconds = 3.4;
        var done = ctx.Render<BlocksView>(p => p.Add(x => x.Blocks, [thinking]));
        Assert.That(done.Find("details.thinking").HasAttribute("open"), Is.False);
        Assert.That(done.Find("summary").TextContent, Is.EqualTo("Thought for 3s"));
    }

    /// <summary>A failed tool is marked failed, with its result.</summary>
    [Test]
    public void AFailedTool_IsMarkedFailed_WithItsResult()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());
        var call = new Block
        {
            Kind = BlockKind.Tool,
            ToolName = "FetchUrl",
            ToolInput = "{\n  \"url\": \"https://x\"\n}",
            ToolResult = "Error: 404",
            ToolFailed = true
        };

        var cut = ctx.Render<BlocksView>(p => p.Add(x => x.Blocks, [call]));

        Assert.That(cut.Find("details.tool").ClassList, Does.Contain("failed"));
        Assert.That(cut.Find(".tool-status").TextContent, Is.EqualTo("✗"));
        Assert.That(cut.Find(".tool-args").TextContent, Is.EqualTo("\"url\": \"https://x\""));
        Assert.That(cut.FindAll("pre").Select(p => p.TextContent), Does.Contain("Error: 404"));
    }
}
