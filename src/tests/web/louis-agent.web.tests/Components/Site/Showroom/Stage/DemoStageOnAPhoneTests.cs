namespace LouisAgent.Web.Tests.Components.Site.Showroom.Stage;

/// <summary>
/// F134.2: on a phone a demo's page keeps K's parts but stacks them. The head's title and lede, side by side on a
/// wide screen, stand one over the other, and what to watch is one chapter under the next.
/// </summary>
public class DemoStageOnAPhoneTests
{
    /// <summary>
    /// A component's rules for a narrow screen: the block its narrow-screen query opens, up to its closing brace.
    /// Empty when it has no such block, so each test says which rule it misses.
    /// </summary>
    private static string PhoneRules(string component)
    {
        string styles = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            $"src/web/louis-agent.web/Components/Site/Showroom/Stage/{component}.razor.scss")).ReplaceLineEndings("\n");
        int start = styles.IndexOf("@media (max-width: 900px) {", StringComparison.Ordinal);

        return start < 0 ? "" : styles[start..styles.IndexOf("\n}", start, StringComparison.Ordinal)];
    }

    /// <summary>The head's two columns are a wide screen's: on a phone the title and the lede stack.</summary>
    [Test]
    public void OnAPhone_TheHeadStacks() =>
        Assert.That(PhoneRules("DemoStage"), Does.Contain(".stage-head { display: block;"));

    /// <summary>The chapters stand side by side on a wide screen and one under the next on a phone.</summary>
    [Test]
    public void OnAPhone_TheChaptersStack() =>
        Assert.That(PhoneRules("DemoCues"), Does.Contain(".cues-list { grid-template-columns: minmax(0, 1fr); }"));
}
