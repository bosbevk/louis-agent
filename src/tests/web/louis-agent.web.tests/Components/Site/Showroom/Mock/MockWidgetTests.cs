using LouisAgent.Web.Components.Site.Showroom.Mock;

namespace LouisAgent.Web.Tests.Components.Site.Showroom.Mock;

/// <summary>
/// F127.2: the agent's chat on a mock website minimises and maximises (Louis: "we should have an minimize and
/// maximize"), as a chat window does: folded to its head so the whole website shows, or over the whole website to read
/// the conversation, and back.
/// </summary>
public class MockWidgetTests
{
    /// <summary>The widget with a label and a chat in it.</summary>
    private static IRenderedComponent<MockWidget> Widget(BunitContext ctx) =>
        ctx.Render<MockWidget>(parameters => parameters
            .Add(widget => widget.Label, "The Bakery · ask us")
            .AddChildContent("<div class=\"chat\">Hello</div>"));

    /// <summary>The widget's button that reads <paramref name="label"/>.</summary>
    private static AngleSharp.Dom.IElement Button(IRenderedComponent<MockWidget> cut, string label) =>
        cut.Find($".mock-widget-head button.window-tool[aria-label='{label}']");

    /// <summary>
    /// Minimised, the chat folds to its head and its button says Restore; pressed again, it comes back.
    /// </summary>
    [Test]
    public void TheChat_Minimises_AndComesBack()
    {
        using var ctx = new BunitContext();
        var cut = Widget(ctx);

        Button(cut, "Minimise").Click();
        var folded = cut.Find(".mock-widget");
        var restore = Button(cut, "Restore");

        Assert.Multiple(() =>
        {
            Assert.That(folded.ClassList, Does.Contain("is-min"));
            Assert.That(restore.GetAttribute("aria-pressed"), Is.EqualTo("true"));
        });

        restore.Click();
        Assert.That(cut.Find(".mock-widget").ClassList, Does.Not.Contain("is-min"));
    }

    /// <summary>
    /// Maximised, the chat covers the website and its button says Restore; pressed again, it comes back.
    /// </summary>
    [Test]
    public void TheChat_Maximises_AndComesBack()
    {
        using var ctx = new BunitContext();
        var cut = Widget(ctx);

        Button(cut, "Maximise").Click();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".mock-widget").ClassList, Does.Contain("is-max"));
            Assert.That(Button(cut, "Restore").GetAttribute("aria-pressed"), Is.EqualTo("true"));
        });

        Button(cut, "Restore").Click();
        Assert.That(cut.Find(".mock-widget").ClassList, Does.Not.Contain("is-max"));
    }

    /// <summary>Minimising a maximised chat folds it: one size at a time.</summary>
    [Test]
    public void Minimising_AMaximisedChat_FoldsIt()
    {
        using var ctx = new BunitContext();
        var cut = Widget(ctx);

        Button(cut, "Maximise").Click();
        Button(cut, "Minimise").Click();

        Assert.That(cut.Find(".mock-widget").ClassList.Where(c => c.StartsWith("is-")),
            Is.EqualTo(new[] { "is-min" }));
    }
}
