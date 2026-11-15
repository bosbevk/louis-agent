namespace LouisAgent.Web.Tests.Components.Widget.Bar;

/// <summary>
/// F134.4: the whole talk, opened, is a drawer at the side of the page on a wide screen, and a sheet over the bar on a
/// phone. Read from the bar's styles, since where a box stands is the styles' work.
/// </summary>
public class AgentTalkDrawerTests
{
    private static string Styles => File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
        "src/web/louis-agent.web/Components/Widget/Bar/AgentBar.razor.scss")).ReplaceLineEndings("\n");

    /// <summary>
    /// The block a rule or a query opens, up to its closing brace at the line's start; empty when there is none.
    /// </summary>
    private static string Block(string opening)
    {
        string styles = Styles;
        int start = styles.IndexOf("\n" + opening + " {", StringComparison.Ordinal);
        return start < 0 ? "" : styles[start..styles.IndexOf("\n}", start + 1, StringComparison.Ordinal)];
    }

    /// <summary>The open talk's rule inside a query's block; empty when it has none.</summary>
    private static string OpenTalkIn(string query)
    {
        string block = Block(query);
        int start = block.IndexOf(".agent-talk.is-open {", StringComparison.Ordinal);
        return start < 0 ? "" : block[start..block.IndexOf('}', start)];
    }

    /// <summary>
    /// On a wide screen the open talk is fixed to the screen's right side, from under the header to the bar's foot.
    /// It can only be, if nothing in the bar moves it by a transform, which would hold it inside the bar.
    /// </summary>
    [Test]
    public void OnAWideScreen_TheOpenTalkIsADrawerAtTheSide()
    {
        string drawer = OpenTalkIn("@media (min-width: 901px)");

        Assert.Multiple(() =>
        {
            Assert.That(drawer, Does.Contain("position: fixed;"));
            Assert.That(drawer, Does.Contain("top:").And.Contain("right:").And.Contain("bottom:"));
            Assert.That(Block(".agent-bar"), Does.Not.Contain("transform"));
        });
    }

    /// <summary>On a phone the open talk stays a sheet over the bar, in its flow, never fixed to a side.</summary>
    [Test]
    public void OnAPhone_TheOpenTalkIsASheetOverTheBar()
    {
        string sheet = OpenTalkIn("@media (max-width: 900px)");

        Assert.Multiple(() =>
        {
            Assert.That(sheet, Does.Contain("max-height:"));
            Assert.That(sheet, Does.Not.Contain("position: fixed"));
        });
    }
}
