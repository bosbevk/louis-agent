using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Chat.Panels;
using LouisAgent.Web.Text;
using LouisAgent.Web.Testing;
using Microsoft.Extensions.DependencyInjection;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Tests.Components.Chat.Panels;

/// <summary>
/// The work a chat handed out. Its words are English unless a site gives its own, and a site brings its own pictures.
/// </summary>
public class WorkPanelTests
{
    /// <summary>Before any work it says so, in English, with no picture unless the site gives one.</summary>
    [Test]
    public void NoWork_SaysSo_InEnglish_WithNoPicture()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<WorkPanel>();

        Assert.That(cut.Find(".work-title").TextContent, Is.EqualTo("Work"));
        Assert.That(cut.Find(".work-empty strong").TextContent, Is.EqualTo("No work yet"));
        Assert.That(cut.FindAll(".work-empty img"), Is.Empty);
    }

    /// <summary>A site's picture shows in the empty panel.</summary>
    [Test]
    public void ASitesPicture_ShowsInTheEmptyPanel()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());

        var cut = ctx.Render<WorkPanel>(parameters => parameters.Add(panel => panel.EmptyImage, "brand/mark.svg"));

        Assert.That(cut.Find(".work-empty img").GetAttribute("src"), Is.EqualTo("brand/mark.svg"));
    }

    /// <summary>A finished delegation shows its steps and cost in English, with no avatar unless the site gives one.</summary>
    [Test]
    public async Task AFinishedDelegation_ShowsItsStepsAndCost_InEnglish()
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton(new ChatText());
        var turn = await SessionStreamFixture.ReplayAsync();

        var cut = ctx.Render<WorkPanel>(parameters => parameters.Add(panel => panel.Chat, new ChatModel
        {
            SessionId = "s",
            Turns = [turn]
        }));

        Assert.That(cut.Find(".work-label").TextContent, Is.EqualTo("Steps"));
        Assert.That(cut.Find(".work-cost").TextContent, Does.Contain("Cost").And.Contain("requests"));
        Assert.That(cut.Find(".work-foot").TextContent, Does.Contain("This chat").And.Contain("Specialists"));
        Assert.That(cut.FindAll(".work-avatar"), Is.Empty);
    }
}
