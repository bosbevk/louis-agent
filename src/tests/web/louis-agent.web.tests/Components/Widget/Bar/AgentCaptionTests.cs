using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget.Bar;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Bar;

/// <summary>
/// F134.4: when the guide's last answer showed a place, its words stand as a caption over the bar, the guide's name
/// on it and a way into the box under them, where the folded talk stood.
/// </summary>
public class AgentCaptionTests
{
    /// <summary>A call to show the levels on Identities, as the guide makes it.</summary>
    private static Block ShowLevels() => new()
    {
        Kind = BlockKind.Tool,
        ToolName = "ShowPage",
        ToolUseId = "toolu_show",
        ToolInput = """{"path":"/identities/levels"}""",
        ToolResult = "Shown."
    };

    /// <summary>The bar over a runner whose chat holds this one turn, done.</summary>
    private static IRenderedComponent<AgentBar> Bar(BunitContext ctx, Turn turn)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out _);
        ctx.Services.AddSingleton(new ChatText());
        var chat = new LouisAgent.Web.Chats.Chat { SessionId = "sess_fixture", Turns = [turn] };
        var runner = new ChatRunner(api, new ChatStore(ctx.JSInterop.JSRuntime, settings), new ChatText(),
            () => Task.FromResult("sess_fixture"))
        { Chat = chat };
        return ctx.Render<AgentBar>(parameters => parameters
            .Add(bar => bar.Runner, runner)
            .Add(bar => bar.Title, "Louis · AI"));
    }

    /// <summary>A turn that showed the levels and said a line about them.</summary>
    private static Turn ShowedAndSaid() => new()
    {
        UserText = "Where are the levels?",
        Blocks = [ShowLevels(), new Block { Kind = BlockKind.Text, Text = "These are the levels." }]
    };

    /// <summary>
    /// The last answer showed a place, so its words stand as the caption, with the guide's name and a way into the
    /// box; the folded talk gives way to it.
    /// </summary>
    [Test]
    public void AfterShowingAPlace_TheAnswerIsACaption()
    {
        using var ctx = new BunitContext();

        var cut = Bar(ctx, ShowedAndSaid());

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".agent-caption .caption-who").TextContent, Is.EqualTo("Louis · AI"));
            Assert.That(cut.Find(".agent-caption .caption-says").TextContent.Trim(),
                Is.EqualTo("These are the levels."));
            Assert.That(cut.Find(".agent-caption label.caption-ask").GetAttribute("for"), Is.EqualTo("composer-input"));
            Assert.That(cut.FindAll(".agent-talk"), Is.Empty);
        });
    }

    /// <summary>An answer that showed no place stays in the folded talk, with no caption.</summary>
    [Test]
    public void AnAnswerThatShowedNoPlace_StaysInTheTalk()
    {
        using var ctx = new BunitContext();
        var turn = new Turn { UserText = "Hi", Blocks = [new Block { Kind = BlockKind.Text, Text = "Hello." }] };

        var cut = Bar(ctx, turn);

        Assert.That((cut.FindAll(".agent-caption").Count, cut.FindAll(".agent-talk").Count), Is.EqualTo((0, 1)));
    }

    /// <summary>The caption's button opens the whole talk in its place.</summary>
    [Test]
    public void TheCaptionsTalkButton_OpensTheWholeTalk()
    {
        using var ctx = new BunitContext();
        var cut = Bar(ctx, ShowedAndSaid());

        cut.Find(".agent-caption .caption-talk").Click();

        Assert.That((cut.FindAll(".agent-caption").Count, cut.FindAll(".agent-talk.is-open").Count),
            Is.EqualTo((0, 1)));
    }
}
