using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Bar;

/// <summary>
/// F134.3: the bar holds the orb, a line of what the guide is doing, the box, and the talk so far, folded over the
/// box to its end until the visitor opens it.
/// </summary>
public class AgentBarTests
{
    /// <summary>A context with what the widget asks for, over the stub API.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out _);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>The guide's widget as a bar.</summary>
    private static IRenderedComponent<ChatWidget> Bar(BunitContext ctx) =>
        ctx.Render<ChatWidget>(parameters => parameters
            .Add(w => w.Identity, "guide")
            .Add(w => w.Title, "Louis · AI")
            .Add(w => w.Placeholder, "Your question…")
            .Add(w => w.Layout, WidgetLayout.Bar));

    /// <summary>
    /// Before anything is asked the bar is the orb, idle, the guide's name where its doing goes, and the box; there
    /// is no talk yet.
    /// </summary>
    [Test]
    public void Waiting_TheBarIsTheOrbItsNameAndTheBox()
    {
        using var ctx = Context();

        var cut = Bar(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".agent-bar .orb").ClassList, Does.Contain("is-idle"));
            Assert.That(cut.Find(".agent-doing").TextContent, Is.EqualTo("Louis · AI"));
            Assert.That(cut.FindAll(".agent-box #composer-input"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll(".agent-talk"), Is.Empty);
        });
    }

    /// <summary>
    /// Once a question has an answer, the talk stands over the box, folded to its end, and a button opens it whole
    /// and folds it again.
    /// </summary>
    [Test]
    public void AfterAnAnswer_TheTalkStandsFolded_AndOpensWhole()
    {
        using var ctx = Context();
        var cut = Bar(ctx);

        cut.Find("#composer-input").Input("What is this site?");
        cut.Find("button.send").Click();
        // The answer has played out when the orb is idle again, the stream's redraws done.
        cut.WaitForState(() => cut.FindAll(".agent-talk").Count == 1 && cut.FindAll(".orb.is-idle").Count == 1,
            TimeSpan.FromSeconds(10));
        var folded = cut.Find(".agent-talk").ClassList.Contains("is-open");
        cut.Find(".agent-talk-toggle").Click();
        cut.WaitForState(() => cut.Find(".agent-talk").ClassList.Contains("is-open"), TimeSpan.FromSeconds(2));

        Assert.That(folded, Is.False, "folded at first");
    }

    /// <summary>A turn ends in words only when the last thing in it is text with something in it.</summary>
    [Test]
    public void ATurn_EndsInWords_OnlyWhenTextCameLast()
    {
        var thinking = new Turn { Blocks = [new Block { Kind = BlockKind.Thinking, Text = "…" }] };
        var answered = new Turn { Blocks = [new Block { Kind = BlockKind.Text, Text = "Hello." }] };
        var calling = new Turn
        {
            Blocks = [new Block { Kind = BlockKind.Text, Text = "Let me look." }, new Block { Kind = BlockKind.Tool }]
        };

        Assert.That((thinking.EndsInWords, answered.EndsInWords, calling.EndsInWords),
            Is.EqualTo((false, true, false)));
    }
}
