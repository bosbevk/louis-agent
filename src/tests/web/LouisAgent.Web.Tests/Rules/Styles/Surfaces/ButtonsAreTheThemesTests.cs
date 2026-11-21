namespace LouisAgent.Web.Tests.Rules.Styles.Surfaces;

/// <summary>
/// A button on any site is the theme's (FX162, Louis: "make this theme buttons, not our own css"): the framework draws
/// the pill once, the main one as the theme's press and the second as its quiet press, so a site writes no button.
/// </summary>
public class ButtonsAreTheThemesTests
{
    /// <summary>The framework's plain elements, where every site's buttons are drawn.</summary>
    private static string Elements() => File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
        "src/web/LouisAgent.Web/Styles/web/base/_elements.scss"));

    /// <summary>
    /// A panel is the theme's pane (Louis: "why does none of my glass have the light source anymore? that should be
    /// in the theme"): its fill, edge and drawing, and under the pointer the pane's hover drawing, where glass's light
    /// runs round the edge.
    /// </summary>
    [Test]
    public void APanel_IsTheThemesPane() =>
        Assert.That(Elements(), Does.Contain(".panel {").And.Contain("@include surface.pane();"));

    /// <summary>The main buttons are the theme's press, and the second ones its quiet press.</summary>
    [Test]
    public void TheButtons_AreTheThemesPresses()
    {
        string elements = Elements();

        Assert.Multiple(() =>
        {
            Assert.That(elements, Does.Contain(".btn-fill, .btn-solid { @include surface.press(); }"));
            Assert.That(elements,
                Does.Contain(".btn-line, .btn-whatsapp, .btn-linkedin { @include surface.quiet-press(); }"));
        });
    }
}
