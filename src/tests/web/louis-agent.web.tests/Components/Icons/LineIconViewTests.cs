using LouisAgent.Diagrams.Icons;
using LouisAgent.Web.Components.Icons;

namespace LouisAgent.Web.Tests.Components.Icons;

/// <summary>
/// F134.1: a demo's sign was an emoji, and Windows draws an emoji black and uneven (Louis: "this looks off and those
/// icons"). A line icon is a drawing the framework holds, so it looks the same everywhere, in the colour and at the
/// size of the words around it. The drawings are the diagrams' own set (Louis's pick of three: one set, in the
/// diagrams project), so a page and a diagram never hold two copies of an icon.
/// </summary>
public class LineIconViewTests
{
    /// <summary>Every icon the framework names.</summary>
    private static IEnumerable<DiagramIcon> Icons => Enum.GetValues<DiagramIcon>();

    /// <summary>The strokes an icon is drawn with.</summary>
    private static string StrokesOf(DiagramIcon icon)
    {
        using var ctx = new BunitContext();
        var view = ctx.Render<LineIconView>(parameters => parameters.Add(drawn => drawn.Icon, icon));

        return string.Join(' ', view.FindAll("svg.line-icon path").Select(path => path.GetAttribute("d")));
    }

    /// <summary>Every icon has a drawing: strokes on a 24 by 24 grid, which a screen reader passes over.</summary>
    [TestCaseSource(nameof(Icons))]
    public void EveryIcon_IsADrawing_AScreenReaderPassesOver(DiagramIcon icon)
    {
        using var ctx = new BunitContext();

        var view = ctx.Render<LineIconView>(parameters => parameters.Add(drawn => drawn.Icon, icon));

        var svg = view.Find("svg.line-icon");
        Assert.Multiple(() =>
        {
            Assert.That(svg.GetAttribute("aria-hidden"), Is.EqualTo("true"));
            Assert.That(svg.GetAttribute("viewBox"), Is.EqualTo("0 0 24 24"));
            Assert.That(svg.QuerySelectorAll("path").Select(path => path.GetAttribute("d")),
                Is.Not.Empty.And.All.Not.Empty);
        });
    }

    /// <summary>No two icons share a drawing, so a new name given another's strokes by mistake shows up here.</summary>
    [Test]
    public void NoTwoIcons_AreTheSameDrawing() =>
        Assert.That(Icons.Select(StrokesOf), Is.Unique);
}
