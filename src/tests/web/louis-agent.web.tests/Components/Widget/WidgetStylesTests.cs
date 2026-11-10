namespace LouisAgent.Web.Tests.Components.Widget;

/// <summary>
/// F127.2: how the guide's chat draws itself minimised and maximised (Louis: "should also have minimize and
/// maximize"): folded to its head, or a large window; on a phone, where it is a sheet over half the screen, the whole
/// screen.
/// </summary>
public class WidgetStylesTests
{
    private static readonly string Widget = Path.Combine(TestPaths.RepoRoot,
        "src/web/louis-agent.web/Components/Widget/ChatWidget.razor.scss");

    /// <summary>
    /// Minimised, only the head shows; maximised, the panel is a large window, and on a phone the screen.
    /// </summary>
    [Test]
    public void TheChat_FoldsToItsHead_OrGrowsToAWindow()
    {
        string styles = File.ReadAllText(Widget).ReplaceLineEndings("\n");

        Assert.Multiple(() =>
        {
            Assert.That(styles, Does.Contain(".widget-panel.is-min { height: auto; }"));
            Assert.That(styles, Does.Contain(
                ".widget-panel.is-min ::deep .widget-list, .widget-panel.is-min .widget-composer { display: none; }"));
            Assert.That(styles, Does.Contain(
                ".widget-panel.is-max { width: min(960px, calc(100vw - 48px)); height: calc(100vh - 120px); }"));
            Assert.That(styles, Does.Contain("    .widget-panel.is-max { height: 100dvh; border-radius: 0; }"));
        });
    }
}
