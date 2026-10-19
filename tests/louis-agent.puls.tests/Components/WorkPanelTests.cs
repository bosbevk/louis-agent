using LouisAgent.Puls.Components.Chat;
using LouisAgent.Puls.Tests.Fixtures;
using LouisAgent.Puls.Text;
using LouisAgent.Web.Shared.Chat;
using Microsoft.Extensions.DependencyInjection;
using ChatModel = LouisAgent.Web.Shared.Chat.Chat;

namespace LouisAgent.Puls.Tests.Components;

// T1: the Werk panel (F20): the work Puls handed out in a chat, from the recorded turn (the cook's steps, its cost,
// the sources), pinned to WorkPanel.html; and the empty panel before any work.
public class WorkPanelTests
{
    [Test]
    public async Task TheCooksWork_MatchesTheGoldenFile()
    {
        var turn = await SessionStreamFixture.ReplayAsync();
        var chat = new ChatModel { SessionId = "sess_fixture", Title = "Ete vir vier", Turns = [turn] };
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(PulsText.Afrikaans);

        var cut = ctx.Render<WorkPanel>(p => p.Add(x => x.Chat, chat));

        Assert.That(cut.Find(".work-count").TextContent, Is.EqualTo("1"));
        Assert.That(cut.Find(".work-who strong").TextContent, Is.EqualTo("puls-cook"));
        Assert.That(cut.FindAll(".work-step"), Has.Count.EqualTo(turn.Blocks.Single(b => b.ToolName == "StartSubSession").SubTurn!.Blocks.Count(b => b.Kind == BlockKind.Tool)));
        Snapshots.Match("Components/WorkPanel.html", cut.Markup);
    }

    [Test]
    public void NoWorkYet_SaysSo()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(PulsText.Afrikaans);

        var cut = ctx.Render<WorkPanel>(p => p.Add(x => x.Chat, new ChatModel { SessionId = "sess_new" }));

        Assert.That(cut.Find(".work-empty strong").TextContent, Is.EqualTo("Nog geen werk nie"));
        Assert.That(cut.FindAll(".work-item"), Is.Empty);
        Assert.That(cut.FindAll(".work-foot"), Is.Empty, "nothing cost anything yet");
    }
}
