using LouisAgent.Web.Scripts;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Components;
using LouisAgent.Web.Text;
using LouisAgent.Testing;
using LouisAgent.Web.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components;

/// <summary>
/// One turn as the chat shows it, rendered with bUnit from the recorded turn and pinned to a golden file. The component
/// highlights code through JS after each render; the loose JS interop answers that call with nothing, as a browser
/// without the script would.
/// </summary>
public class TurnViewTests
{
    /// <summary>A finished turn matches the golden file.</summary>
    [Test]
    public async Task AFinishedTurn_MatchesTheGoldenFile()
    {
        var turn = await SessionStreamFixture.ReplayAsync();
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<TurnView>(p => p.Add(x => x.Turn, turn).Add(x => x.Streaming, false));

        // The turn's own module (CL31): imported once, asked to highlight the blocks, nothing else.
        Assert.That(ctx.JSInterop.Invocations.Select(i => i.Identifier).Distinct(),
            Is.EqualTo(new[] { "import", "highlightBlocks" }));
        Assert.That(ctx.JSInterop.Invocations.Single(i => i.Identifier == "import").Arguments[0],
            Is.EqualTo(FrameworkScripts.TurnView));
        Snapshots.Match("Components/TurnView.html", cut.Markup);
    }

    /// <summary>
    /// A chat on a business's website shows no usage line (F127.2, Louis: "hide it in the demo"): a customer never
    /// sees tokens; told to show it, a finished turn has one.
    /// </summary>
    [Test]
    public async Task AFinishedTurn_ShowsItsUsage_UnlessToldNot()
    {
        var turn = await SessionStreamFixture.ReplayAsync();
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());

        var shown = ctx.Render<TurnView>(p => p.Add(x => x.Turn, turn));
        var hidden = ctx.Render<TurnView>(p => p.Add(x => x.Turn, turn).Add(x => x.ShowUsage, false));

        Assert.Multiple(() =>
        {
            Assert.That(shown.FindAll(".assistant-message > .usage"), Has.Count.EqualTo(1));
            Assert.That(hidden.FindAll(".assistant-message > .usage"), Is.Empty);
        });
    }

    /// <summary>A streaming turn shows what the specialist is doing.</summary>
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
            {
                [SessionStreamFixture.CookRole] = new("Looking for ingredients", "Building the recipe")
            }
        });

        var cut = ctx.Render<TurnView>(p => p.Add(x => x.Turn, turn).Add(x => x.Streaming, true));

        Assert.That(cut.Find(".working").TextContent, Does.Contain("Looking for ingredients"));
        Assert.That(cut.FindAll(".assistant-message > .usage"), Is.Empty, "no usage line for the turn while it runs");
    }
}
