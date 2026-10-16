using System.Text.RegularExpressions;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;

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
    public void PersonalityAndTheRouter_AreAlwaysThere()
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
    public void AToolsetNameAsTheFunction_DoesntLoadItsGuideOutsideTheProfile()
    {
        // AGENT_FUNCTION=python names a toolset, not a function file: its guide comes only with the toolset.
        var options = Options();
        options.AgentFunction = "python";

        Assert.That(AgentHost.LoadSkills(options, Toolsets("files")).Documentation, Does.Not.Contain("# Python Skills"));
    }

    [Test]
    public void FunctionSkills_ComeWithTheSkillsToolset()
    {
        // The function's skill file (and with "louis", every one) is a set of procedures run by ExecuteSkill: it belongs
        // to the skills toolset, like a guide to its tools.
        var louis = Options();
        louis.AgentFunction = "louis";

        Assert.That(AgentHost.LoadSkills(louis, Toolsets("skills")).Documentation,
            Does.Contain("# Code Review Skills").And.Not.Contain("# Time Logging Skills"));
        Assert.That(AgentHost.LoadSkills(Options(), Toolsets("files", "skills")).Documentation, Does.Contain("# Code Review Skills"));
    }

    [Test]
    public void WithoutTheSkillsToolset_NoFunctionSkills()
    {
        // Puls's chats load only the agents toolset: its prompt is the router and the agents guide, not every skill file
        // in the folder (AGENT_FUNCTION=louis used to add them all, agent-made ones included).
        var louis = Options();
        louis.AgentFunction = "louis";

        string docs = AgentHost.LoadSkills(louis, Toolsets("agents")).Documentation;

        Assert.That(docs, Does.Not.Contain("# Code Review Skills").And.Not.Contain("# Time Logging Skills"));
        Assert.That(AgentHost.LoadSkills(Options(), Toolsets("files")).Documentation, Does.Not.Contain("# Code Review Skills"));
    }

    [Test]
    public void ThePromptShrinks_WithTheToolset()
    {
        int all = AgentHost.LoadSkills(Options(), ToolsetCatalog.All).Documentation.Length;
        int coding = AgentHost.LoadSkills(Options(), Toolsets("files", "git", "dotnet", "skills")).Documentation.Length;

        Assert.That(coding, Is.LessThan(all));
    }
}
