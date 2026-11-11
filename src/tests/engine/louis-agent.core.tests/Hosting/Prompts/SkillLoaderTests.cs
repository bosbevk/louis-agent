using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using System.Text.RegularExpressions;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Skills;

namespace LouisAgent.Core.Tests.Hosting.Prompts;

/// <summary>
/// The system prompt has the guides of the session's toolsets and no others, so the model is never told about tools it
/// doesn't have, and the prompt shrinks with the toolset (F5 step 4, F5-S2).
/// </summary>
public class SkillLoaderTests
{
    /// <summary>Guides a files-and-git prompt must not carry.</summary>
    private static readonly string[] OtherGuides =
    [
        "# Python Skills", "# PowerShell Skills", "# Bash Skills", "# Web Skills"
    ];

    /// <summary>The tests' own skills folder, with the test product's English voice as the default.</summary>
    private static AgentOptions Options() => new()
    {
        SkillsDirectory = TestPaths.Skills,
        DefaultVoice = "testprod/voice-en"
    };

    /// <summary>The test catalogue's toolsets by these names.</summary>
    private static IReadOnlyList<ToolsetDefinition> Toolsets(params string[] names) =>
        TestTools.Catalog.Toolsets.Where(t => names.Contains(t.Name)).ToList();

    /// <summary>Files and git bring their guides, and no other toolset's.</summary>
    [Test]
    public void FilesAndGit_HaveTheirGuides_AndNoOthers()
    {
        string docs = SkillLoader.Load(Options(), Toolsets(TestToolset.Files.Wire(), TestToolset.Git.Wire()))
            .Documentation;

        Assert.That(docs, Does.Contain("# Files Skills").And.Contain("# Git Skills"));
        foreach (string guide in OtherGuides)
            Assert.That(docs, Does.Not.Contain(guide), $"{guide} isn't in a files+git prompt");
    }

    /// <summary>
    /// The skills folder is the one set, else the nearest marked folder above: no built-in file is on disk any more,
    /// so the folder the agent writes into is found by its README (CL8 step 4).
    /// </summary>
    [Test]
    public void TheSkillsFolder_IsSkillsDirectory_ElseASkillsFolderAbove()
    {
        string demo = Directory.CreateTempSubdirectory("skills-root-").FullName;
        string marker = Path.GetFullPath(Path.Combine(demo, SkillFiles.MarkedFolderPath));
        string skills = Directory.CreateDirectory(Path.GetDirectoryName(marker)!).FullName;
        File.WriteAllText(marker, "");
        string deep = Directory.CreateDirectory(Path.Combine(demo, "one", "two")).FullName;
        try
        {
            var told = new AgentOptions { SkillsDirectory = "told" };
            Assert.That(SkillFolder.Find(told, deep), Is.EqualTo("told"));
            Assert.That(SkillFolder.Find(new AgentOptions(), deep), Is.EqualTo(skills));
        }
        finally
        {
            Directory.Delete(demo, recursive: true);
        }
    }

    /// <summary>
    /// The toolset that hands work on is called specialists (CL9 step 5; Louis: "this should say specialists since we
    /// call them that now"), F30's minions get their own, and <c>agents</c>, its name before CL9, is since F127.3.1 the
    /// toolset that asks another agent by its address, with a guide of its own.
    /// </summary>
    [Test]
    public void TheToolsetThatHandsWorkOn_IsCalledSpecialists()
    {
        var specialists = ToolsetCatalog.Find(CoreToolset.Specialists.Wire());
        var agents = ToolsetCatalog.Find(CoreToolset.Agents.Wire());

        Assert.That(specialists?.GuideFile, Is.EqualTo("toolsets/specialists.md"));
        Assert.That(agents?.GuideFile, Is.EqualTo("toolsets/agents.md"));
    }

    /// <summary>
    /// Every agent, at every level, whatever its voice or role, gets the shared principles first, so no later file can
    /// push them aside (CL9 step 2).
    /// </summary>
    [TestCase(null, RoleLevel.Agent)]
    [TestCase("testprod/voice-af", RoleLevel.Agent)]
    [TestCase("testprod-specialist", RoleLevel.Specialist)]
    [TestCase(null, RoleLevel.Specialist)]
    [TestCase("testprod-minion-lookup", RoleLevel.Minion)] // F30: a minion always has a role
    public void EveryPrompt_StartsWithThePrinciples(string? role, RoleLevel level)
    {
        string docs = SkillLoader.Load(Options(), [], role, level).Documentation;

        Assert.That(docs, Does.StartWith("# Principles"));
        Assert.That(docs, Does.Contain("An agent may never lie, cheat, steal or murder."));
    }

    /// <summary>With no toolsets the voice and its router are still there, and no toolset's guide.</summary>
    [Test]
    public void TheRoleAndTheRouter_AreAlwaysThere()
    {
        string docs = SkillLoader.Load(Options(), []).Documentation;

        Assert.That(docs, Does.Contain("# Testprod - Routing"), "default.md, the toolset router");
        Assert.That(docs.Length, Is.GreaterThan(0));
        foreach (string guide in OtherGuides)
            Assert.That(docs, Does.Not.Contain(guide));
    }

    /// <summary>With every toolset, each guide is in the prompt once.</summary>
    [Test]
    public void EveryToolset_GivesEachGuideExactlyOnce()
    {
        string docs = SkillLoader.Load(Options(), TestTools.Catalog.Toolsets).Documentation;

        foreach (string guide in OtherGuides.Append("# Files Skills").Append("# Git Skills").Append("# .NET Skills"))
            Assert.That(Regex.Matches(docs, "^" + Regex.Escape(guide) + @"\r?$", RegexOptions.Multiline).Count,
                Is.EqualTo(1), guide);
    }

    /// <summary>
    /// A function skill is a set of procedures run by ExecuteSkill: it belongs to the skills toolset, like a guide to
    /// its tools, so a prompt without that toolset has no skill file (CL9: there's no setting to add one).
    /// </summary>
    [Test]
    public void FunctionSkills_ComeWithTheSkillsToolset_AndNotWithout()
    {
        string skills = Directory.CreateTempSubdirectory("function-skills-").FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(skills, "agent", "functions"));
            File.WriteAllText(Path.Combine(skills, "agent", "functions", "greeting.md"),
                "# Greeting Skills\n\nGreets people.");
            var options = new AgentOptions { SkillsDirectory = skills };

            Assert.That(
                SkillLoader.Load(options, Toolsets(TestToolset.Files.Wire(), CoreToolset.Skills.Wire())).Documentation,
                Does.Contain("# Greeting Skills"));
            Assert.That(
                SkillLoader.Load(options, Toolsets(CoreToolset.Specialists.Wire(), TestToolset.Web.Wire()))
                    .Documentation, Does.Not.Contain("# Greeting Skills"));
        }
        finally
        {
            Directory.Delete(skills, recursive: true);
        }
    }

    /// <summary>
    /// The addresses an identity may ask (F127.3.1) come with the agents toolset, after its guide, so the model knows
    /// whom it may ask; without the toolset, or with no addresses, nothing is listed.
    /// </summary>
    [Test]
    public void TheAddressesItMayAsk_ComeWithTheAgentsToolset()
    {
        string[] calls = ["acme", "acme.cook"];

        string asks = SkillLoader.Load(Options(), Toolsets(CoreToolset.Agents.Wire()), calls: calls).Documentation;
        string none = SkillLoader.Load(Options(), Toolsets(TestToolset.Files.Wire()), calls: calls).Documentation;
        string empty = SkillLoader.Load(Options(), Toolsets(CoreToolset.Agents.Wire()), calls: []).Documentation;

        Assert.Multiple(() =>
        {
            Assert.That(asks,
                Does.Contain("# Agents you may ask").And.Contain("- `acme`").And.Contain("- `acme.cook`"));
            Assert.That(asks.IndexOf("# Agents you may ask", StringComparison.Ordinal),
                Is.GreaterThan(asks.IndexOf("# Agents Skills", StringComparison.Ordinal)), "after the agents guide");
            Assert.That(none, Does.Not.Contain("# Agents you may ask"));
            Assert.That(empty, Does.Not.Contain("# Agents you may ask"));
        });
    }

    /// <summary>A session with fewer toolsets reads a shorter prompt.</summary>
    [Test]
    public void ThePromptShrinks_WithTheToolset()
    {
        int all = SkillLoader.Load(Options(), TestTools.Catalog.Toolsets).Documentation.Length;
        int coding = SkillLoader.Load(Options(),
            Toolsets(TestToolset.Files.Wire(), TestToolset.Git.Wire(), TestToolset.DotNet.Wire(),
                CoreToolset.Skills.Wire())).Documentation.Length;

        Assert.That(coding, Is.LessThan(all));
    }
}
