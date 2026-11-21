namespace LouisAgent.Web.Tests.Rules.Styles.Surfaces;

/// <summary>
/// A site's menu is one glass rail (FX162, Louis's pick C of three): the menu sits in the theme's group, as the look
/// switch does; a link under the pointer gets the quiet button's hover glass, an open group is the main button, and so
/// is the item under the pointer in its list. All of it is the theme's, so a site writes none of it.
/// </summary>
public class TheMenuIsAGlassRailTests
{
    /// <summary>One of the framework's own sheets, by its path under the web project.</summary>
    private static string Sheet(string path) =>
        File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web", path));

    /// <summary>The menu is the group, and each link in it a choice in the rail.</summary>
    [Test]
    public void TheMenu_IsTheThemesGroup_AndEachLinkAChoice()
    {
        string shell = Sheet("Components/Site/SiteShell.razor.scss");

        Assert.Multiple(() =>
        {
            Assert.That(shell, Does.Contain("@include surface.group();"));
            Assert.That(shell, Does.Contain(".site-nav ::deep a { padding: 7px 13px; @include surface.rail-choice(); }"));
        });
    }

    /// <summary>A group's label is a choice, open it is the main button, and so is the item under the pointer.</summary>
    [Test]
    public void AnOpenGroup_AndTheItemUnderThePointer_AreTheMainButton()
    {
        string menu = Sheet("Components/Site/Menu/SiteMenu.razor.scss");

        Assert.Multiple(() =>
        {
            Assert.That(menu, Does.Contain("@include surface.rail-choice();"));
            Assert.That(menu, Does.Contain(
                ".site-drop:hover .site-drop-label, .site-drop:focus-within .site-drop-label { @include surface.press(); }"));
            Assert.That(menu, Does.Contain(".site-drop-list ::deep a:hover { @include surface.press(); }"));
        });
    }

    /// <summary>
    /// The page the visitor is on stays chosen (Louis: "must stay selected if I'm on that page"): its link is the main
    /// button, in the rail or in a group's list, and so is the label of the group that holds it.
    /// </summary>
    [Test]
    public void ThePageYouAreOn_IsTheMainButton()
    {
        string shell = Sheet("Components/Site/SiteShell.razor.scss");
        string menu = Sheet("Components/Site/Menu/SiteMenu.razor.scss");

        Assert.Multiple(() =>
        {
            Assert.That(shell, Does.Contain(".site-nav ::deep a.active { @include surface.press(); }"));
            Assert.That(menu, Does.Contain(".site-drop-list ::deep a.active { @include surface.press(); }"));
            Assert.That(menu, Does.Contain(".site-drop:has(a.active) .site-drop-label { @include surface.press(); }"));
        });
    }

    /// <summary>A choice in a rail lays the quiet button's hover glass under the pointer, and a tint where none is.</summary>
    [Test]
    public void AChoice_UnderThePointer_IsTheQuietButtonsHoverGlass() =>
        Assert.That(Sheet("Styles/web/tools/surface/_presses.scss"),
            Does.Contain("@mixin rail-choice()").And.Contain("drawn(quiet-press-hover"));
}
