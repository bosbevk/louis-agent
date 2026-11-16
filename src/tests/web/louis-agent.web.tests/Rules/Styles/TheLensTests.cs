using LouisAgent.Testing;

namespace LouisAgent.Web.Tests.Rules.Styles;

/// <summary>
/// The lens (F151.6, Louis's way 2): a theme that bends light through glass bends it only in the card under the
/// pointer, or the one picked or shown. A pane says it is such a card, and how, in its own styles, and the frame's
/// module does the bending, so no component wires it by hand.
/// </summary>
public class TheLensTests
{
    private static readonly string Web = Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web");

    /// <summary>The surface tool's panes, where a pane, a picked pane and a shown one are written.</summary>
    private static string Panes() =>
        File.ReadAllText(Path.Combine(Web, "Styles/web/tools/surface/_panes.scss"));

    /// <summary>What a mixin of the panes writes, from its name to the next mixin.</summary>
    private static string Mixin(string name)
    {
        var panes = Panes();
        int start = panes.IndexOf($"@mixin {name}(", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"the panes write {name}");
        int end = panes.IndexOf("@mixin ", start + 1, StringComparison.Ordinal);
        return end < 0 ? panes[start..] : panes[start..end];
    }

    /// <summary>A pane may wear the lens, a picked pane wears it white, a shown one violet.</summary>
    [TestCase("pane", "--web-lens-piece: 1")]
    [TestCase("opened", "--web-lens-piece: 2")]
    [TestCase("picked", "--web-lens-piece: 2")]
    [TestCase("showing", "--web-lens-piece: 3")]
    public void APane_SaysHowItWearsTheLens(string mixin, string mark) =>
        Assert.That(Mixin(mixin), Does.Contain(mark));

    /// <summary>
    /// A pane at rest lays the theme's light under its drawing, where glass paints the bend its lens would give, and
    /// lays none under the pointer, where the lens itself takes over.
    /// </summary>
    [Test]
    public void APaneAtRest_LaysTheThemesLight()
    {
        var pane = Mixin("pane");
        int hover = pane.IndexOf("&:hover", StringComparison.Ordinal);
        Assert.Multiple(() =>
        {
            Assert.That(pane[..hover], Does.Contain("var(--web-pane-light)"));
            Assert.That(pane[hover..], Does.Not.Contain("var(--web-pane-light)"));
        });
    }

    /// <summary>The mark belongs to the piece that sets it, never to what the piece holds.</summary>
    [Test]
    public void TheMark_IsNotHandedDown() =>
        Assert.That(File.ReadAllText(Path.Combine(Web, "Styles/web/base/_elements.scss")),
            Does.Contain("@property --web-lens-piece").And.Contain("inherits: false;"));

    /// <summary>The frame's module brings the lens to every page of every site.</summary>
    [Test]
    public void TheFrame_BringsTheLens() =>
        Assert.That(File.ReadAllText(Path.Combine(Web, "Components/Site/SiteShell.razor.js")),
            Does.Contain("import { lens } from \"../../js/lens.js\";"));
}
