namespace LouisAgent.Core.Tests.Projects;

/// <summary>
/// F166.2 step 7: the workflow that proves a project outside builds an agent from the published packages, restoring
/// them from GitHub Packages with nothing but the run's own token, as Puls will (the plan's done-when).
/// </summary>
/// <remarks>
/// Plain string checks on the file's lines, as <c>PublishWorkflowTests</c> holds the publish (ADR 0057).
/// </remarks>
public class ConsumeWorkflowTests
{
    /// <summary>The workflow's file.</summary>
    private static readonly string WorkflowPath =
        Path.Combine(TestPaths.RepoRoot, ".github/workflows/consume-packages.yml");

    /// <summary>Its lines, trimmed; none when the file isn't there, so each test fails on what it holds.</summary>
    private static List<string> Lines() =>
        File.Exists(WorkflowPath) ? [.. File.ReadAllLines(WorkflowPath).Select(line => line.Trim())] : [];

    /// <summary>
    /// It runs by hand, and on a pull request that changes it, so a change is proved before it merges.
    /// </summary>
    [Test]
    public void ItRunsByHand_AndWhenItChanges()
    {
        var lines = Lines();

        Assert.Multiple(() =>
        {
            Assert.That(lines, Does.Contain("workflow_dispatch:"));
            Assert.That(lines, Does.Contain("- '.github/workflows/consume-packages.yml'"));
        });
    }

    /// <summary>It may read packages and nothing more, and its only key is the run's own token.</summary>
    [Test]
    public void ItReadsPackages_WithTheRunsTokenAlone()
    {
        var lines = Lines();

        Assert.Multiple(() =>
        {
            Assert.That(lines, Does.Contain("packages: read"));
            Assert.That(lines, Has.None.Contains("packages: write"));
            Assert.That(lines, Has.Some.Contains("secrets.GITHUB_TOKEN"));
            Assert.That(lines.Where(line => line.Contains("secrets.")),
                Is.All.Contains("secrets.GITHUB_TOKEN"), "no other secret");
        });
    }

    /// <summary>
    /// It knows only the feed: a project outside the repository restores the engine and the test helpers from GitHub
    /// Packages and runs an agent, and the run checks the agent's answer.
    /// </summary>
    [Test]
    public void AProjectOutside_RestoresFromTheFeed_AndRunsAnAgent()
    {
        var lines = Lines();

        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Some.Contains("https://nuget.pkg.github.com/bosbevk/index.json"));
            Assert.That(lines, Has.Some.Contains("Include=\"LouisAgent.Core\"")
                .And.Some.Contains("Include=\"LouisAgent.Testing\""));
            Assert.That(lines, Has.Some.Contains("new AgentEngine("));
            Assert.That(lines, Has.Some.Contains("grep -q \"hello from the packages\""));
        });
    }
}
