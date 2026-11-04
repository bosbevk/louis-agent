namespace LouisAgent.Core.Tests;

/// <summary>
/// The folders on disk say what the solution says: a project in the solution's <c>src</c> group <c>engine</c> sits in
/// <c>src/engine/</c>, so someone in a terminal and someone in Rider see the same map, and the engine's side of the split
/// is one folder.
/// </summary>
public class SolutionLayoutTests
{
    /// <summary>What marks a solution folder in the solution file, rather than a project.</summary>
    private const string SolutionFolder = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

    /// <summary>Each group holds the projects whose folder on disk has that name.</summary>
    [TestCase("src/engine", "louis-agent.core", "louis-agent.choices", "louis-agent.session-stream")]
    [TestCase("src/hosts", "louis-agent.acp-server", "louis-agent.api", "louis-agent.api.contracts", "louis-agent.cli",
        "louis-agent.composition", "louis-agent.host-packs", "louis-agent.mcp-server", "louis-agent.orchestrator")]
    [TestCase("src/web", "louis-agent.puls", "louis-agent.site", "louis-agent.web", "louis-agent.web.shared")]
    public void AGroup_IsAFolderOnDisk(string folder, params string[] projects)
    {
        var lines = File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, "louis-agent-solution.sln"));
        string group = folder.Split('/')[1];

        // Who sits in the group is the solution's nesting section; where each one is on disk is its own line.
        string groupId = IdOf(lines.Single(line => line.StartsWith($"Project(\"{SolutionFolder}\") = \"{group}\"")));
        var nested = lines.Where(line => line.Trim().EndsWith($"= {groupId}"))
            .Select(line => line.Trim().Split(' ')[0]).ToHashSet();
        var found = lines.Where(line => line.StartsWith("Project(") && nested.Contains(IdOf(line)))
            .Select(line => line.Split('"')[5].Replace('\\', '/')).ToList();

        Assert.That(found, Is.EquivalentTo(projects.Select(name => $"{folder}/{name}/{name}.csproj")));
    }

    /// <summary>What proves a pack or an identity sits under <c>evals/</c> on disk, apart from what it proves.</summary>
    [TestCase("louis-agent.pack.recipes.checks", "evals/packs")]
    [TestCase("louis-agent.pack.where-to-buy.checks", "evals/packs")]
    [TestCase("louis-agent.identity.puls.evals", "evals/identities")]
    [TestCase("louis-agent.identity.puls.judges", "evals/identities")]
    public void WhatProvesAPackOrAnIdentity_IsUnderTheEvals(string project, string folder)
    {
        var lines = File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, "louis-agent-solution.sln"));

        string path = lines.Single(line => line.StartsWith("Project(") && line.Contains($"= \"{project}\","))
            .Split('"')[5].Replace('\\', '/');

        Assert.That(path, Is.EqualTo($"{folder}/{project}/{project}.csproj"));
    }

    /// <summary>The id at the end of a solution file's project line.</summary>
    private static string IdOf(string line) => line.Split('"')[^2];
}
