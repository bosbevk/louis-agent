namespace LouisAgent.Core.Tests.Projects;

/// <summary>
/// F166.2: the workflow that publishes the framework's packages. A version tag starts it and nothing else does; it
/// sees the history MinVer needs, packs the projects the commit's tags name and never the whole solution, and pushes
/// to GitHub Packages with the run's own token (Louis's picks C, A and the updated A).
/// </summary>
/// <remarks>
/// Plain string checks on the file's lines, as <c>DeployWorkflowTests</c> holds the deploy (ADR 0057).
/// </remarks>
public class PublishWorkflowTests
{
    /// <summary>The workflow's file.</summary>
    private static readonly string WorkflowPath =
        Path.Combine(TestPaths.RepoRoot, ".github/workflows/publish-packages.yml");

    /// <summary>Its lines, trimmed; none when the file isn't there, so each test fails on what it holds.</summary>
    private static List<string> Lines() =>
        File.Exists(WorkflowPath) ? [.. File.ReadAllLines(WorkflowPath).Select(line => line.Trim())] : [];

    /// <summary>A package's tag starts it, and no branch, pull request or button does.</summary>
    [Test]
    public void OnlyAPackagesTag_StartsIt()
    {
        var lines = Lines();

        Assert.Multiple(() =>
        {
            Assert.That(lines, Does.Contain("tags: [ 'LouisAgent.*-*' ]"));
            Assert.That(lines, Has.None.StartsWith("branches:").And.None.StartsWith("pull_request:")
                .And.None.StartsWith("workflow_dispatch:"));
        });
    }

    /// <summary>It may write packages and only read the code, and it sees every tag, as MinVer needs.</summary>
    [Test]
    public void ItWritesPackages_AndSeesTheTags()
    {
        var lines = Lines();

        Assert.Multiple(() =>
        {
            Assert.That(lines, Does.Contain("packages: write"));
            Assert.That(lines, Does.Contain("contents: read"));
            Assert.That(lines, Does.Contain("fetch-depth: 0"));
        });
    }

    /// <summary>
    /// It packs the projects the commit's tags name, one by one, never the solution, and pushes them to this account's
    /// feed with the run's own token; a package already there is skipped, so the run for a second tag on the same
    /// commit has nothing left to do.
    /// </summary>
    [Test]
    public void ItPacksTheTaggedProjects_AndPushesWithTheRunsToken()
    {
        var lines = Lines();

        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Some.Contains("git tag --points-at HEAD"));
            Assert.That(lines, Has.Some.StartsWith("dotnet pack \"$project\""));
            Assert.That(lines, Has.None.Contains("dotnet pack louis-agent-solution.sln"));
            Assert.That(lines, Has.Some.Contains("https://nuget.pkg.github.com/bosbevk/index.json")
                .And.Some.Contains("secrets.GITHUB_TOKEN").And.Some.Contains("--skip-duplicate"));
        });
    }

    /// <summary>
    /// Before anything is pushed, every framework package a packed one depends on is in the same batch or already on
    /// the feed at the version it names; one that is neither stops the run (Louis's updated pick A, the guard).
    /// </summary>
    [Test]
    public void TheGuard_ChecksEachFrameworkDependency_BeforeThePush()
    {
        var lines = Lines();
        int guard = lines.FindIndex(line => line == "- name: Check every framework package they need is published");
        int push = lines.FindIndex(line => line == "- name: Push them to GitHub Packages");

        Assert.Multiple(() =>
        {
            Assert.That(guard, Is.GreaterThanOrEqualTo(0).And.LessThan(push), "the guard runs before the push");
            Assert.That(lines, Has.Some.Contains("unzip -p").And.Some.Contains("<dependency id=\"LouisAgent\\."));
            Assert.That(lines, Has.Some.Contains("out/$name.$version.nupkg"), "a package of the same batch counts");
            Assert.That(lines, Has.Some.Contains("users/bosbevk/packages/nuget/$name/versions"), "so does the feed");
            Assert.That(lines, Has.Some.Contains("exit 1"));
        });
    }
}
