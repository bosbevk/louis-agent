using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Scripts;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Door;

/// <summary>
/// F164.2 step 5: a button in a site's header that opens its agent's chat, "Ask Orbi" on the framework's site, with
/// Ctrl+I or Cmd+I from anywhere on the page, as a docs site's assistant opens.
/// </summary>
public class OpenAgentButtonTests
{
    /// <summary>A context with a door, whose browser answers the button's module.</summary>
    private static BunitContext Context(GuideDoor door)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(door);
        return ctx;
    }

    /// <summary>A press opens the chat through the door with no question, so nothing is asked for them.</summary>
    [Test]
    public void APress_OpensTheChat_WithNoQuestion()
    {
        var door = new GuideDoor();
        using var ctx = Context(door);
        var heard = new List<string?>();
        door.Asked += heard.Add;

        var cut = ctx.Render<OpenAgentButton>(parameters => parameters.Add(button => button.Text, "Ask Orbi"));
        cut.Find("button").Click();

        Assert.Multiple(() =>
        {
            Assert.That(heard, Is.EqualTo(new string?[] { null }));
            Assert.That(cut.Find("button").TextContent, Does.Contain("Ask Orbi"));
            Assert.That(cut.Find("button").GetAttribute("type"), Is.EqualTo("button"));
        });
    }

    /// <summary>
    /// F169.3: the button wears the mark before its words (Louis's pick 5), in a part a screen reader skips, since it
    /// already hears the words.
    /// </summary>
    [Test]
    public void TheButton_WearsTheMark_BeforeItsWords()
    {
        using var ctx = Context(new GuideDoor());

        var cut = ctx.Render<OpenAgentButton>(parameters => parameters.Add(button => button.Text, "Ask Orbi"));
        var button = cut.Find("button");
        var mark = button.QuerySelector(".open-agent-mark");

        Assert.Multiple(() =>
        {
            Assert.That(button.QuerySelectorAll(".orb"), Has.Length.EqualTo(1));
            Assert.That(button.FirstElementChild, Is.SameAs(mark), "the mark comes before the words");
            Assert.That(mark!.GetAttribute("aria-hidden"), Is.EqualTo("true"));
            Assert.That(mark.QuerySelector(".orb"), Is.Not.Null);
        });
    }

    /// <summary>
    /// Its keys are said to a screen reader, and held by the framework's shortcut module, which presses the button on
    /// Ctrl+I or Cmd+I.
    /// </summary>
    [Test]
    public void ItsKeys_AreSaid_AndHeldByTheShortcutModule()
    {
        using var ctx = Context(new GuideDoor());
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.Shortcut);
        module.SetupVoid("attach", _ => true);

        var cut = ctx.Render<OpenAgentButton>(parameters => parameters.Add(button => button.Text, "Ask Orbi"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("button").GetAttribute("aria-keyshortcuts"), Is.EqualTo("Control+I Meta+I"));
            Assert.That(module.Invocations["attach"].Single().Arguments[1], Is.EqualTo("i"));
        });
    }
}
