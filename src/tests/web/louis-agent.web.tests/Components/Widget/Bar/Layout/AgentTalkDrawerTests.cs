namespace LouisAgent.Web.Tests.Components.Widget.Bar.Layout;

/// <summary>
/// F134.4: the whole talk is a drawer at the side of the page on a wide screen, and a sheet over the bar on a phone.
/// Read from the talk's styles and the bar's, since where a box stands is the styles' work.
/// </summary>
public class AgentTalkDrawerTests
{
    private static readonly string Bar =
        Path.Combine(TestPaths.RepoRoot, "src/web/louis-agent.web/Components/Widget/Bar");

    /// <summary>A file of the bar's, its line ends folded.</summary>
    private static string Read(string path) => File.ReadAllText(Path.Combine(Bar, path)).ReplaceLineEndings("\n");

    /// <summary>
    /// The block a rule or a query opens in these styles, up to its closing brace at the line's start; empty when
    /// there is none.
    /// </summary>
    private static string Block(string styles, string opening)
    {
        int start = styles.IndexOf("\n" + opening + " {", StringComparison.Ordinal);
        return start < 0 ? "" : styles[start..styles.IndexOf("\n}", start + 1, StringComparison.Ordinal)];
    }

    /// <summary>The talk's rule inside a query's block of its own styles; empty when it has none.</summary>
    private static string TalkIn(string query)
    {
        string block = Block(Read("Talk/AgentTalk.razor.scss"), query);
        int start = block.IndexOf(".agent-talk {", StringComparison.Ordinal);
        return start < 0 ? "" : block[start..block.IndexOf('}', start)];
    }

    /// <summary>
    /// On a wide screen the talk is fixed to the screen's right side, from under the header to the bar's foot. It
    /// can only be, if nothing in the bar moves it by a transform, which would hold it inside the bar.
    /// </summary>
    [Test]
    public void OnAWideScreen_TheTalkIsADrawerAtTheSide()
    {
        string drawer = TalkIn("@media (min-width: 901px)");

        Assert.Multiple(() =>
        {
            Assert.That(drawer, Does.Contain("position: fixed;"));
            Assert.That(drawer, Does.Contain("top:").And.Contain("right:").And.Contain("bottom:"));
            Assert.That(Block(Read("AgentBar.razor.scss"), ".agent-bar"), Does.Contain("position: fixed;")
                .And.Not.Contain("transform"));
        });
    }

    /// <summary>On a phone the talk stays a sheet over the bar, in its flow, never fixed to a side.</summary>
    [Test]
    public void OnAPhone_TheTalkIsASheetOverTheBar()
    {
        string sheet = TalkIn("@media (max-width: 900px)");

        Assert.Multiple(() =>
        {
            Assert.That(sheet, Does.Contain("max-height:"));
            Assert.That(sheet, Does.Not.Contain("position: fixed"));
        });
    }
}
