using System.Reflection;
using LouisAgent.Choices;
using LouisAgent.Core.Skills;
using LouisAgent.Packs.Docs;
using LouisAgent.Packs.Docs.Tools;

namespace LouisAgent.Packs.Docs.Tests;

/// <summary>The pack is one toolset with its guide, off until its one setting is set, and shaped like every pack.</summary>
public class DocsPackTests
{
    /// <summary>Its name, its toolset and its guide are the pack's own, and the guide says what comes here.</summary>
    [Test]
    public void TheGuide_ComesFromThePack_NotFromCore()
    {
        var pack = new DocsPack(new DocsOptions());

        Assert.That(pack.Name, Is.EqualTo(DocsToolset.Docs.Wire()));
        Assert.That(pack.Toolsets.Select(t => t.Name), Is.EqualTo(new[] { DocsToolset.Docs.Wire() }));
        Assert.That(pack.Toolsets[0].GuideFile, Is.EqualTo("toolsets/docs.md"));
        Assert.That(SkillFileSource.CoreFiles, Has.None.EqualTo("toolsets/docs.md"), "core's don't hold it");
        Assert.That(SkillFileSource.ReadEmbedded("toolsets/docs.md"),
            Does.Contain("Route here any question").And.Not.Contain("## Skill:"));
    }

    /// <summary>Without DOCS_ROOT the toolset says what it lacks; with it, it loads and its tools may run side by side.</summary>
    [Test]
    public void ItNeedsTheDocsRoot_AndRunsInParallel()
    {
        var off = new DocsPack(new DocsOptions()).Toolsets[0];
        var on = new DocsPack(new DocsOptions { Root = Path.GetTempPath() }).Toolsets[0];

        Assert.Multiple(() =>
        {
            Assert.That(off.MissingSettings(null!), Is.EqualTo(new[] { "DOCS_ROOT" }));
            Assert.That(on.MissingSettings(null!), Is.Empty);
            Assert.That(on.ParallelSafe, Is.True);
            Assert.That(on.ToolTypes, Is.EqualTo(new[] { typeof(DocsTools) }));
            Assert.That(on.CreateTools(null!).Single(), Is.TypeOf<DocsTools>());
        });
    }

    /// <summary>The setting is read from the environment, trimmed, and an empty one is none.</summary>
    [TestCase("  /app/docs ", "/app/docs")]
    [TestCase("", null)]
    [TestCase(null, null)]
    public void TheSetting_IsReadFromTheEnvironment(string? value, string? expected) =>
        Assert.That(DocsOptions.FromEnvironment(_ => value).Root, Is.EqualTo(expected));

    /// <summary>
    /// Every tool is a public method with a description, taking plain values and returning text: the shape that can be
    /// served over MCP as it is (ADR 0028).
    /// </summary>
    [Test]
    public void EveryTool_IsDescribed_AndTakesPlainValues()
    {
        var tools = typeof(DocsTools).GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.That(tools.Select(t => t.Name), Is.EquivalentTo(new[] { "ListDocs", "ReadDoc", "SearchDocs" }));
        Assert.That(tools, Has.All.Matches<MethodInfo>(t =>
            t.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>() is not null));
        Assert.That(tools, Has.All.Matches<MethodInfo>(t => t.ReturnType == typeof(string)));
        Assert.That(tools.SelectMany(t => t.GetParameters()),
            Has.All.Matches<ParameterInfo>(p => p.ParameterType == typeof(string) || p.ParameterType == typeof(int)));
    }
}
