using LouisAgent.Web.Testing;
using LouisAgent.Testing;
using LouisAgent.Web.Shared.Chats;
using LouisAgent.Web.Shared.Components;
using LouisAgent.Web.Shared.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Shared.Tests.Components;

// T1: one turn as the chat shows it, rendered with bUnit from the recorded Puls turn and pinned to TurnView.html. The
// component highlights code through JS after each render (louisChat.highlightBlocks); the loose JS interop answers
// that call with nothing, as a browser without the script would.
public class TurnViewTests
{
    [Test]
    public async Task AFinishedTurn_MatchesTheGoldenFile()
    {
        var turn = await SessionStreamFixture.ReplayAsync();
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<TurnView>(p => p.Add(x => x.Turn, turn).Add(x => x.Streaming, false));

        Assert.That(ctx.JSInterop.Invocations.Select(i => i.Identifier).Distinct(),
            Is.EqualTo(new[] { "louisChat.highlightBlocks" }));
        Snapshots.Match("louis-agent.web.shared.tests", "Components/TurnView.html", cut.Markup);
    }

    [Test]
    public async Task AStreamingTurn_ShowsWhatTheSpecialistIsDoing()
    {
        var turn = await SessionStreamFixture.ReplayAsync();
        // Still running: no stop or usage yet, the cook's call has no result, and its own last step is a search.
        turn.StopReason = null;
        turn.Usage = null;
        var call = turn.Blocks.Single(b => b.ToolName == "StartSubSession");
        call.ToolResult = null;
        turn.Blocks.RemoveAll(b => b != call);
        call.SubTurn!.Blocks.RemoveAll(b => b.Kind != BlockKind.Tool);
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText
        {
            RolePhases = new Dictionary<string, RolePhases>
            { [SessionStreamFixture.CookRole] = new("Looking for ingredients", "Building the recipe") }
        });

        var cut = ctx.Render<TurnView>(p => p.Add(x => x.Turn, turn).Add(x => x.Streaming, true));

        Assert.That(cut.Find(".working").TextContent, Does.Contain("Looking for ingredients"));
        Assert.That(cut.FindAll(".assistant-message > .usage"), Is.Empty, "no usage line for the turn while it runs");
    }
}
