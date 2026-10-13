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
    public void ThePromptShrinks_WithTheToolset()
    {
        int all = AgentHost.LoadSkills(Options(), ToolsetCatalog.All).Documentation.Length;
        int coding = AgentHost.LoadSkills(Options(), Toolsets("files", "git", "dotnet", "skills")).Documentation.Length;

        Assert.That(coding, Is.LessThan(all));
    }
}
