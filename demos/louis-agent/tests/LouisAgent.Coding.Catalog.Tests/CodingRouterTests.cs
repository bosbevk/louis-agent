using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Identities.Coding;

namespace LouisAgent.Coding.Tests;

/// <summary>How the coding agent's mode sends work to its toolsets, as every session reads it.</summary>
public class CodingRouterTests
{
    /// <summary>The router's text, from the identity's own files.</summary>
    private static string Routing()
    {
        var identity = new CodingIdentity();
        using var reader = new StreamReader(identity.Prompts
            .GetFileInfo(RoleCatalog.FileName(identity.DefaultVoice, RoleLevel.Agent)).CreateReadStream());
        return RoleCatalog.ModeParts(reader.ReadToEnd()).Routing;
    }

    /// <summary>
    /// The router names every toolset and describes no tools, so the prompt never shows a session tools it doesn't
    /// load.
    /// </summary>
    [Test]
    public void TheRouter_NamesEveryToolsetAndDescribesNoTools()
    {
        string doc = Routing();
        var catalog = CodingPrompts.Catalog;

        // Every toolset but the ones of packs another identity names (F109.3: the expert's docs are not the coding
        // agent's to route); there is no minions toolset (F127.5: AskAgent asks minions too).
        var coding = catalog.Identities[0];
        var othersToolsets = catalog.Packs.Where(p => !coding.Packs.Contains(p.Name))
            .SelectMany(p => p.Toolsets.Select(t => t.Name)).ToHashSet(StringComparer.Ordinal);
        foreach (string toolset in catalog.Toolsets.Select(t => t.Name)
                     .Where(name => !othersToolsets.Contains(name)))
            Assert.That(doc, Does.Contain($"`{toolset}`"), $"the router should name the '{toolset}' toolset");
        Assert.That(doc, Does.Not.Contain("`minions`"), "a chat never holds minions");

        string[] tools =
        [
            "ReadWorkspaceFile", "WriteWorkspaceFile", "GetStatus", "Commit(", "PythonRun", "WebSearch",
            "CreateSkillFile",
            "LogTimeByTaskName", "GetWorkItemsByAssignee",
        ];
        foreach (string tool in tools)
            Assert.That(doc, Does.Not.Contain(tool), $"the router should not describe the {tool} tool");
    }

    /// <summary>A product's routing holds its own rows, so neither pays for the other's.</summary>
    [Test]
    public void TheRouter_HoldsNothingOfAnotherProducts() =>
        Assert.That(Routing(), Does.Not.Contain("**Vir:**"), "no Afrikaans briefing");

    /// <summary>How to review a change is in the mode: there's no skill for it.</summary>
    [Test]
    public void CodeReview_IsPartOfTheCodingAgentsMode()
    {
        Assert.That(Routing(), Does.Contain("Reviewing a change").And.Contain("`git`"));
        Assert.That(SkillFileSource.ReadEmbedded(SkillFiles.Function("code-review")), Is.Null);
    }
}
