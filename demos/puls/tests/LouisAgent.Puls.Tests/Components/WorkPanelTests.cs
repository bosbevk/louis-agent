using LouisAgent.Web.Components.Chat.Panels;
using LouisAgent.Puls.Text;
using LouisAgent.Web.Chats;
using Microsoft.Extensions.DependencyInjection;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Puls.Tests.Components;

/// <summary>
/// The work panel: the work Puls handed out in a chat, from the recorded turn (the cook's steps, its cost, the
/// sources), pinned to WorkPanel.html; and the empty panel before any work.
/// </summary>
public class WorkPanelTests
{
    /// <summary>The cook's work matches the golden file.</summary>
    [Test]
    public async Task TheCooksWork_MatchesTheGoldenFile()
    {
        var turn = await SessionStreamFixture.ReplayAsync();
        var chat = new ChatModel
        {
            SessionId = "sess_fixture",
            Title = "Ete vir vier",
            Turns = [turn]
        };
        await using var ctx = new BunitContext();
        ctx.Services.AddSingleton(PulsText.Afrikaans);

        var cut = ctx.Render<WorkPanel>(p => p
            .Add(x => x.Chat, chat)
            .Add(x => x.EmptyImage, "brand/puls-mark.svg")
            .Add(x => x.Avatar, "brand/puls-icon-tile.svg"));

        Assert.That(cut.Find(".work-count").TextContent, Is.EqualTo("1"));
        Assert.That(cut.Find(".work-who strong").TextContent, Is.EqualTo(SessionStreamFixture.CookAddress));
        Assert.That(cut.FindAll(".work-step"),
            Has.Count.EqualTo(
                turn.Blocks.Single(b => b.ToolName == Delegation.AskToolName).SubTurn!.Blocks.Count(b =>
                    b.Kind == BlockKind.Tool)));
        Snapshots.Match("Components/WorkPanel.html", cut.Markup);
    }

    /// <summary>Before any work, the panel says so.</summary>
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
