using System.Reflection;
using LouisAgent.Choices;
using LouisAgent.Core.Skills;
using LouisAgent.Packs.Triage.Tools;

namespace LouisAgent.Packs.Triage.Tests;

/// <summary>The pack is one toolset with its guide, off until its two settings are set, and shaped like every pack.</summary>
public class TriagePackTests
{
    /// <summary>Its name, its toolset and its guide are the pack's own, and the guide says how to work.</summary>
    [Test]
    public void TheGuide_ComesFromThePack_NotFromCore()
    {
        var pack = new TriagePack(new TriageOptions());

        Assert.That(pack.Name, Is.EqualTo(TriageToolset.Triage.Wire()));
        Assert.That(pack.Toolsets.Select(t => t.Name), Is.EqualTo(new[] { TriageToolset.Triage.Wire() }));
        Assert.That(pack.Toolsets[0].GuideFile, Is.EqualTo("toolsets/triage.md"));
        Assert.That(SkillFileSource.CoreFiles, Has.None.EqualTo("toolsets/triage.md"), "core's don't hold it");
        Assert.That(SkillFileSource.ReadEmbedded("toolsets/triage.md"),
            Does.Contain("ReportInvestigation").And.Contain("ReadTriageRun").And.Not.Contain("## Skill:"));
    }

    /// <summary>Without its settings the toolset says what it lacks; with them it loads both tool classes, one at a time.</summary>
    [Test]
    public void ItNeedsItsThreeSettings_AndRunsOneCallAtATime()
    {
        var off = new TriagePack(new TriageOptions()).Toolsets[0];
        var half = new TriagePack(new TriageOptions { Runs = Path.GetTempPath() }).Toolsets[0];
        var on = new TriagePack(new TriageOptions
        {
            Runs = Path.GetTempPath(),
            People = Path.GetTempPath(),
            Departments = Path.Combine(TestPaths.RepoRoot, "demos/louis-agent/config/departments.yaml")
        }).Toolsets[0];

        Assert.Multiple(() =>
        {
            Assert.That(off.MissingSettings(null!),
                Is.EqualTo(new[] { "TRIAGE_RUNS", "TRIAGE_PEOPLE", "TRIAGE_DEPARTMENTS" }));
            Assert.That(half.MissingSettings(null!), Is.EqualTo(new[] { "TRIAGE_PEOPLE", "TRIAGE_DEPARTMENTS" }));
            Assert.That(on.MissingSettings(null!), Is.Empty);
            Assert.That(on.ParallelSafe, Is.False);
            Assert.That(on.ToolTypes, Is.EqualTo(new[] { typeof(TriageTools), typeof(RunTools) }));
            Assert.That(on.CreateTools(null!).Select(tool => tool.GetType()),
                Is.EqualTo(new[] { typeof(TriageTools), typeof(RunTools) }));
        });
    }

    /// <summary>The settings are read from the environment, trimmed, and an empty one is none.</summary>
    [TestCase("  /data/runs ", "/data/runs")]
    [TestCase("", null)]
    [TestCase(null, null)]
    public void ASetting_IsReadFromTheEnvironment(string? value, string? expected)
    {
        var options = TriageOptions.FromEnvironment(_ => value);

        Assert.That(options.Runs, Is.EqualTo(expected));
        Assert.That(options.People, Is.EqualTo(expected));
    }

    /// <summary>
    /// Every tool is a public method with a description, taking plain values and a token where it waits, returning
    /// text: the shape that can be served over MCP as it is (ADR 0028).
    /// </summary>
    [Test]
    public void EveryTool_IsDescribed_AndTakesPlainValues()
    {
        var tools = new[] { typeof(TriageTools), typeof(RunTools) }
            .SelectMany(type =>
                type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .ToList();

        Assert.That(tools.Select(t => t.Name),
            Is.EquivalentTo(new[]
            {
                "SortCase", "ReportInvestigation", "ReportReview", "DecideCase", "RaiseToPerson", "ReadTriageRun"
            }));
        Assert.That(tools, Has.All.Matches<MethodInfo>(t =>
            t.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>() is not null));
        Assert.That(tools, Has.All.Matches<MethodInfo>(t => t.ReturnType == typeof(Task<string>)));
        Assert.That(tools.SelectMany(t => t.GetParameters()), Has.All.Matches<ParameterInfo>(p =>
            p.ParameterType == typeof(string) || p.ParameterType == typeof(bool) ||
            p.ParameterType == typeof(CancellationToken)));
        Assert.That(tools, Has.All.Matches<MethodInfo>(t =>
                t.GetParameters().Any(p => p.ParameterType == typeof(CancellationToken))),
            "a tool that waits takes a token");
    }
}
