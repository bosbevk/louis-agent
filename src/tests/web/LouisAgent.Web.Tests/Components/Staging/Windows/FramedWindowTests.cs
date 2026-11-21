using LouisAgent.Web.Components.Staging.Windows;

namespace LouisAgent.Web.Tests.Components.Staging.Windows;

/// <summary>
/// F134.1: on the stage a demo is shown as the thing itself, a window like a browser's. The frame is a part of its
/// own that knows nothing of demos: a bar with an address word, then whatever body it is given.
/// </summary>
public class FramedWindowTests
{
    /// <summary>A window at this address, lit or not, with one thing in it.</summary>
    private static IRenderedComponent<FramedWindow> Window(BunitContext ctx, bool lit = false) =>
        ctx.Render<FramedWindow>(parameters => parameters
            .Add(window => window.Address, "seemeeu.example")
            .Add(window => window.Lit, lit)
            .AddChildContent("<p class=\"shown\">The website</p>"));

    /// <summary>
    /// The bar holds the address word and its three dots, which are decoration. The body holds what it is given.
    /// </summary>
    [Test]
    public void TheFrame_HoldsItsAddressWord_AndItsBody()
    {
        using var ctx = new BunitContext();

        var window = Window(ctx).Find(".framed-window");

        Assert.Multiple(() =>
        {
            Assert.That(window.QuerySelector(".window-bar .window-address")!.TextContent,
                Is.EqualTo("seemeeu.example"));
            Assert.That(window.QuerySelector(".window-bar .window-dots")!.GetAttribute("aria-hidden"),
                Is.EqualTo("true"));
            Assert.That(window.QuerySelectorAll(".window-dots i"), Has.Count.EqualTo(3));
            Assert.That(window.QuerySelector(".window-body .shown")!.TextContent, Is.EqualTo("The website"));
            Assert.That(window.QuerySelector(".window-bar .shown"), Is.Null);
        });
    }

    /// <summary>A window in the light is marked so, and one that is not carries no mark.</summary>
    [TestCase(true, "framed-window is-lit")]
    [TestCase(false, "framed-window")]
    public void AWindowInTheLight_IsMarked(bool lit, string classes)
    {
        using var ctx = new BunitContext();

        Assert.That(Window(ctx, lit).Find(".framed-window").ClassName, Is.EqualTo(classes));
    }
}
