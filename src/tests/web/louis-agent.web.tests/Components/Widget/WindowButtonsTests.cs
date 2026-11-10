using LouisAgent.Web.Components.Widget;

namespace LouisAgent.Web.Tests.Components.Widget;

/// <summary>
/// F127.2: the two buttons a chat window has, shared by the guide's widget and a mock website's chat (Louis: "we
/// should have an minimize and maximize"). Each asks for its own size, or back to normal while its size is on, and
/// says Restore then; the chat is in one size at a time.
/// </summary>
public class WindowButtonsTests
{
    /// <summary>The buttons at <paramref name="size"/>, and the sizes they asked for.</summary>
    private static IRenderedComponent<WindowButtons> Buttons(BunitContext ctx, ChatWindowSize size,
        List<ChatWindowSize> asked) =>
        ctx.Render<WindowButtons>(parameters => parameters
            .Add(buttons => buttons.Size, size)
            .Add(buttons => buttons.SizeChanged, (ChatWindowSize wanted) => asked.Add(wanted)));

    /// <summary>
    /// The first button minimises, the second maximises; pressed while its own size is on, it asks for normal again.
    /// </summary>
    [TestCase(ChatWindowSize.Normal, 0, ChatWindowSize.Minimised)]
    [TestCase(ChatWindowSize.Minimised, 0, ChatWindowSize.Normal)]
    [TestCase(ChatWindowSize.Maximised, 0, ChatWindowSize.Minimised)]
    [TestCase(ChatWindowSize.Normal, 1, ChatWindowSize.Maximised)]
    [TestCase(ChatWindowSize.Maximised, 1, ChatWindowSize.Normal)]
    [TestCase(ChatWindowSize.Minimised, 1, ChatWindowSize.Maximised)]
    public void AButton_AsksForItsSize_OrBackToNormal(ChatWindowSize size, int button, ChatWindowSize wanted)
    {
        using var ctx = new BunitContext();
        var asked = new List<ChatWindowSize>();

        Buttons(ctx, size, asked).FindAll("button.window-tool")[button].Click();

        Assert.That(asked, Is.EqualTo(new[] { wanted }));
    }

    /// <summary>While its size is on, a button says Restore and is pressed, for a screen reader too.</summary>
    [TestCase(ChatWindowSize.Normal, "Minimise", "Maximise", "false", "false")]
    [TestCase(ChatWindowSize.Minimised, "Restore", "Maximise", "true", "false")]
    [TestCase(ChatWindowSize.Maximised, "Minimise", "Restore", "false", "true")]
    public void AButton_SaysRestore_WhileItsSizeIsOn(ChatWindowSize size, string first, string second,
        string firstPressed, string secondPressed)
    {
        using var ctx = new BunitContext();

        var buttons = Buttons(ctx, size, []).FindAll("button.window-tool");

        Assert.That(buttons.Select(b => (b.GetAttribute("aria-label"), b.GetAttribute("aria-pressed"))),
            Is.EqualTo(new[] { (first, firstPressed), (second, secondPressed) }));
    }
}
