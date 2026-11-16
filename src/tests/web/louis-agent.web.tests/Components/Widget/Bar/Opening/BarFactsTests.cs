using LouisAgent.Web.Components.Widget.Bar.Opening;

namespace LouisAgent.Web.Tests.Components.Widget.Bar.Opening;

/// <summary>
/// FX172: the bar starts closed, the orb alone, and opens only while one side needs the other (Louis: "the agent must
/// start closed, then only when it request a answer from you or you from him", his pick A of three).
/// </summary>
public class BarFactsTests
{
    /// <summary>Nothing going on: the bar is closed.</summary>
    [Test]
    public void WithNothingGoingOn_TheBarIsClosed() =>
        Assert.That(new BarFacts(false, false, false, false, false).Open, Is.False);

    /// <summary>Each one alone opens it: held, the guide answering, writing, a piece picked, an answer standing.</summary>
    [TestCase(true, false, false, false, false)]
    [TestCase(false, true, false, false, false)]
    [TestCase(false, false, true, false, false)]
    [TestCase(false, false, false, true, false)]
    [TestCase(false, false, false, false, true)]
    public void AnyOneNeed_OpensIt(bool held, bool answering, bool writing, bool picked, bool answered) =>
        Assert.That(new BarFacts(held, answering, writing, picked, answered).Open, Is.True);

    /// <summary>
    /// Closed, the orb stands in the middle (Louis: "must be centered"), and what stands on each side is attached to
    /// the dock's edges, so it sits right beside the orb and rides out with the box (Louis: "the must attach").
    /// </summary>
    [Test]
    public void TheClosedOrb_StandsCentred_WithItsSidesAttached()
    {
        string styles = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Widget/Bar/AgentBar.razor.scss"));

        Assert.Multiple(() =>
        {
            Assert.That(styles, Does.Contain(".agent-dock {\n    position: relative;\n    max-width: 100%;\n    margin-inline: auto;")
                .Or.Contain(".agent-dock {\r\n    position: relative;\r\n    max-width: 100%;\r\n    margin-inline: auto;"));
            Assert.That(styles, Does.Contain(".agent-bar.is-closed .agent-dock { max-width: calc(76px + 2 * 14px); }"));
        });
    }
}
