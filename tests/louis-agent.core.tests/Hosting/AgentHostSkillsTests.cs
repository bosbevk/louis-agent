using System.Text.RegularExpressions;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Tests.Hosting;

// F5 step 4 (F5-S2): the system prompt has the guides of the session's toolsets and no others, so the model is never
// told about tools it doesn't have, and the prompt shrinks with the toolset.
public class AgentHostSkillsTests
{
    private static readonly string[] OtherGuides =
        ["# Python Skills", "# PowerShell Skills", "# Bash Skills", "# Web Skills", "# Paymo Skills", "# DevOps Skills"];

    private static AgentOptions Options() => new() { SkillsDirectory = Path.Combine(TestPaths.RepoRoot, "Skills") };

    private static IReadOnlyList<ToolsetDefinition> Toolsets(params string[] names) =>
        ToolsetCatalog.All.Where(t => names.Contains(t.Name)).ToList();

    [Test]
    public void FilesAndGit_HaveTheirGuides_AndNoOthers()
    {
        string docs = AgentHost.LoadSkills(Options(), Toolsets("files", "git")).Documentation;

        Assert.That(docs, Does.Contain("# Files Skills").And.Contain("# Git Skills"));
        foreach (string guide in OtherGuides)
            Assert.That(docs, Does.Not.Contain(guide), $"{guide} isn't in a files+git prompt");
    }

    [Test]
    public void TheSkillsFolder_IsSkillsDirectory_ElseTheReposSkillsFolder()
    {
        // CL8 step 4: no built-in file is on disk any more, so the folder the agent writes into is found by its README.
        string skills = Directory.CreateTempSubdirectory("skills-root-").FullName;
        try
        {
            Assert.That(AgentHost.FindSkillsDirectory(new AgentOptions { SkillsDirectory = skills }), Is.EqualTo(skills));
            Assert.That(AgentHost.FindSkillsDirectory(new AgentOptions()), Is.EqualTo(Path.Combine(TestPaths.RepoRoot, "Skills")),
                "from the tests' folder up, not tests/louis-agent.core.tests/Skills/");
        }
        finally
        {
            Directory.Delete(skills, recursive: true);
        }
    }

    [Test]
    public void TheToolsetThatHandsWorkOn_IsCalledSpecialists()
    {
        // CL9 step 5 (Louis: "this should say specialists since we call them that now"); F30's minions get their own.
        var specialists = ToolsetCatalog.Find("specialists");

        Assert.That(specialists?.GuideFile, Is.EqualTo("toolsets/specialists.md"));
        Assert.That(ToolsetCatalog.Find("agents"), Is.Null, "the old name is gone");
    }

    [TestCase(null, RoleLevel.Agent)]
    [TestCase("puls/voice-af", RoleLevel.Agent)]
    [TestCase("puls-cook", RoleLevel.Specialist)]
    [TestCase(null, RoleLevel.Specialist)]
    [TestCase("puls-minion-web-price-lookup", RoleLevel.Minion)]   // F30: a minion always has a role
    public void EveryPrompt_StartsWithThePrinciples(string? role, RoleLevel level)
    {
        // CL9 step 2: every agent, at every level, whatever its voice or role, gets the shared principles first, so no
        // later file can push them aside.
        string docs = AgentHost.LoadSkills(Options(), [], role, level).Documentation;

        Assert.That(docs, Does.StartWith("# Principles"));
        Assert.That(docs, Does.Contain("An agent may never lie, cheat, steal or murder."));
    }

    [Test]
    public void TheRoleAndTheRouter_AreAlwaysThere()
    {
        string docs = AgentHost.LoadSkills(Options(), []).Documentation;

        Assert.That(docs, Does.Contain("# Louis Agent - Default Instructions"), "default.md, the toolset router");
        Assert.That(docs.Length, Is.GreaterThan(0));
        foreach (string guide in OtherGuides)
            Assert.That(docs, Does.Not.Contain(guide));
    }

    [Test]
    public void EveryToolset_GivesEachGuideExactlyOnce()
    {
        string docs = AgentHost.LoadSkills(Options(), ToolsetCatalog.All).Documentation;

        foreach (string guide in OtherGuides.Append("# Files Skills").Append("# Git Skills").Append("# .NET Skills"))
            Assert.That(Regex.Matches(docs, "^" + Regex.Escape(guide) + @"\r?$", RegexOptions.Multiline).Count, Is.EqualTo(1), guide);
    }

    [Test]
    public void FunctionSkills_ComeWithTheSkillsToolset_AndNotWithout()
    {
        // A function skill is a set of procedures run by ExecuteSkill: it belongs to the skills toolset, like a guide to its
        // tools. Puls's chats load agents and web, so their prompt has no skill file (CL9: there's no setting to add one).
        string skills = Directory.CreateTempSubdirectory("function-skills-").FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(skills, "agent", "functions"));
            File.WriteAllText(Path.Combine(skills, "agent", "functions", "greeting.md"), "# Greeting Skills\n\nGreets people.");
            var options = new AgentOptions { SkillsDirectory = skills };

            Assert.That(AgentHost.LoadSkills(options, Toolsets("files", "skills")).Documentation, Does.Contain("# Greeting Skills"));
            Assert.That(AgentHost.LoadSkills(options, Toolsets("specialists", "web")).Documentation, Does.Not.Contain("# Greeting Skills"));
        }
        finally
        {
            Directory.Delete(skills, recursive: true);
        }
    }

    [Test]
    public void ThePromptShrinks_WithTheToolset()
    {
        int all = AgentHost.LoadSkills(Options(), ToolsetCatalog.All).Documentation.Length;
        int coding = AgentHost.LoadSkills(Options(), Toolsets("files", "git", "dotnet", "skills")).Documentation.Length;

        Assert.That(coding, Is.LessThan(all));
    }
}
