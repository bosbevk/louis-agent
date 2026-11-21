using LouisAgent.Diagrams.Icons;
using LouisAgent.Web.Components.Staging.Windows;

namespace LouisAgent.Web.Tests.Components.Staging.Windows;

/// <summary>
/// F134.1: a window with nothing of its own to show still says what it is. The plain body is an icon, large, over a
/// name and a line, so no window on a stage is ever empty.
/// </summary>
public class PlainBodyTests
{
    /// <summary>The icon is a line drawing and decoration. The name and the line are what is read.</summary>
    [Test]
    public void TheBody_IsItsIcon_OverItsNameAndItsLine()
    {
        using var ctx = new BunitContext();

        var body = ctx.Render<PlainBody>(parameters => parameters
            .Add(plain => plain.Icon, DiagramIcon.Code)
            .Add(plain => plain.Name, "A coder")
            .Add(plain => plain.Line, "Fixes a bug")).Find(".plain-body");

        Assert.Multiple(() =>
        {
            Assert.That(body.QuerySelector(".plain-icon")!.GetAttribute("aria-hidden"), Is.EqualTo("true"));
            Assert.That(body.QuerySelectorAll(".plain-icon svg.line-icon"), Has.Count.EqualTo(1));
            Assert.That(body.QuerySelector(".plain-name")!.TextContent, Is.EqualTo("A coder"));
            Assert.That(body.QuerySelector(".plain-line")!.TextContent, Is.EqualTo("Fixes a bug"));
        });
    }
}
