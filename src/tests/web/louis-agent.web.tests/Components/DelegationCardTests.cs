using LouisAgent.Web.Chats;
using LouisAgent.Web.Components;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components;

/// <summary>
/// The delegation card: the cook's card from the recorded turn, done, pinned to a golden file; and the states a card
/// goes through (running, done, failed) as the chat draws them.
/// </summary>
public class DelegationCardTests
{
    /// <summary>The cook's call in the recorded turn, and what its card reads from it.</summary>
    private static (Block Block, Delegation Delegation) TheCooksCall(Turn turn)
    {
        var block = turn.Blocks.Single(b => b.ToolName == "StartSubSession");
        return (block, Delegation.From(block)!);
    }

    /// <summary>The cook's finished card matches the golden file.</summary>
    [Test]
    public async Task TheCooksCard_Done_MatchesTheGoldenFile()
    {
        var (block, delegation) = TheCooksCall(await SessionStreamFixture.ReplayAsync());
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<DelegationCard>(p => p.Add(x => x.Delegation, delegation).Add(x => x.Block, block));

        Assert.That(cut.Find("details.delegation").ClassList, Does.Contain("done"));
        Assert.That(cut.Find(".delegation-agent strong").TextContent, Is.EqualTo(SessionStreamFixture.CookRole),
            "the role, as the user thinks of it");
        Snapshots.Match("Components/DelegationCard.html", cut.Markup);
    }

    /// <summary>
    /// The card for the recorded ask (F127.3.1): done, named for the business the guide asked, and pinned to a golden
    /// file.
    /// </summary>
    [Test]
    public async Task TheAskedBusinesssCard_Done_MatchesTheGoldenFile()
    {
        var turn = await SessionStreamFixture.ReplayAskAsync();
        var block = turn.Blocks.Single(b => b.ToolName == Delegation.AskToolName);
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<DelegationCard>(p =>
            p.Add(x => x.Delegation, Delegation.From(block)!).Add(x => x.Block, block));

        Assert.That(cut.Find("details.delegation").ClassList, Does.Contain("done"));
        Assert.That(cut.Find(".delegation-agent strong").TextContent, Is.EqualTo("seemeeu-guesthouse"),
            "the address, as the guide asked it");
        Snapshots.Match("Components/DelegationCard.ask.html", cut.Markup);
    }

    /// <summary>A running card is open and says it's working.</summary>
    [Test]
    public async Task ARunningCard_IsOpen_AndSaysItIsWorking()
    {
        var (block, _) = TheCooksCall(await SessionStreamFixture.ReplayAsync());
        block.ToolResult = null;
        var delegation = Delegation.From(block)!;
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<DelegationCard>(p =>
            p.Add(x => x.Delegation, delegation).Add(x => x.Block, block).Add(x => x.Streaming, true));

        Assert.That(cut.Find("details.delegation").ClassList, Does.Contain("running"));
        Assert.That(cut.Find("details.delegation").HasAttribute("open"), Is.True);
        Assert.That(cut.Find(".delegation-status").TextContent, Is.EqualTo("Working"));
        Assert.That(cut.FindAll(".delegation-cost"), Is.Empty, "the cost comes with the rolled-up line");
    }

    /// <summary>A follow-up's chip just says it's a follow-up; the handle used inside stays out of sight.</summary>
    [Test]
    public void AFollowUp_ShowsOnlyTheFollowUpChip_NotTheHandle()
    {
        // A continue call, which carries the handle of the specialist it goes back to.
        var block = new Block
        {
            Kind = BlockKind.Tool,
            ToolUseId = "call_2",
            ToolName = "ContinueSubSession",
            ToolInput = """{"handle":"sub-1","message":"En die nagereg?"}""",
            ToolResult = "Malva poeding."
        };
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<DelegationCard>(p =>
            p.Add(x => x.Delegation, Delegation.From(block)!).Add(x => x.Block, block));

        Assert.That(cut.Find(".toolset-chip.follow-up").TextContent, Is.EqualTo("follow-up"));
    }

    /// <summary>A failed call is marked failed, with the error.</summary>
    [Test]
    public void AFailedCall_IsMarkedFailed_WithTheError()
    {
        var block = new Block
        {
            Kind = BlockKind.Tool,
            ToolUseId = "call_1",
            ToolName = "StartSubSession",
            ToolInput =
                """{"agent":"puls","toolsets":["web"],"message":"Kook","role":"<RoleAlpha>"}""".Replace("<RoleAlpha>",
                    SessionStreamFixture.CookRole),
            ToolResult = "Error: no peer named puls",
            ToolFailed = true
        };
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<DelegationCard>(p =>
            p.Add(x => x.Delegation, Delegation.From(block)!).Add(x => x.Block, block));

        Assert.That(cut.Find("details.delegation").ClassList, Does.Contain("failed"));
        Assert.That(cut.Find(".delegation-status").TextContent, Is.EqualTo("Failed"));
        Assert.That(cut.Find(".delegation-answer").TextContent, Does.Contain("Error: no peer named puls"));
    }
}
