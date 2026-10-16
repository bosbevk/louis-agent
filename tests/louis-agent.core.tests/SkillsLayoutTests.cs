using LouisAgent.Core.Agent;
using LouisAgent.Core.Skills;

namespace LouisAgent.Core.Tests;

// docs/CLAUDE.md, "Skill files follow the levels" (CL8, ADR 0016 and 0017): a built-in prompt lives in core's Prompts/,
// in the folder of the level that reads it, or in toolsets/ when every level shares it; the repo's Skills/ holds only
// what the agent writes. Read from the folders, so a misplaced file fails here before a session ever loads it.
public class SkillsLayoutTests
{
    /// <summary>The folders under Prompts/ a built-in file may be in: a level's, its function skills', or the shared guides'.</summary>
    private static readonly string[] PromptFolders =
        [Personalities.AgentFolder, SkillFiles.FunctionsFolder, Personalities.SpecialistsFolder, Personalities.MinionsFolder, SkillFiles.ToolsetsFolder];

    private static readonly SkillLevel[] Levels = Enum.GetValues<SkillLevel>();

    private static IEnumerable<string> FilesUnder(string root) =>
        Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            : [];

    [Test]
    public void EveryPrompt_IsInAKnownFolder()
    {
        var misplaced = FilesUnder(TestPaths.Prompts).Where(f => !PromptFolders.Contains(Path.GetDirectoryName(f)!.Replace('\\', '/')));

        Assert.That(misplaced, Is.Empty, "Prompts/ holds agent/, agent/functions/, specialists/, minions/ and toolsets/ only");
    }

    [Test]
    public void EveryPrompt_IsEmbedded_AndNothingElseIs()
    {
        Assert.That(SkillFileSource.EmbeddedFiles, Is.EquivalentTo(FilesUnder(TestPaths.Prompts)), "the csproj embeds Prompts/** and only that");
    }

    [Test]
    public void NoPersonality_IsInTwoLevels()
    {
        // A role name means one file, so PERSONALITIES_ENABLED names roles without a level.
        var twice = Levels.SelectMany(Personalities.Available).GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key);

        Assert.That(twice, Is.Empty);
    }

    [Test]
    public void EveryToolset_HasItsGuideInToolsets_AndEveryGuideItsToolset()
    {
        var guides = ToolsetCatalog.All.Select(t => t.GuideFile).ToList();

        Assert.That(guides, Has.All.StartsWith(SkillFiles.ToolsetsFolder + "/"));
        Assert.That(SkillFileSource.Files(SkillFiles.ToolsetsFolder).Select(f => $"{SkillFiles.ToolsetsFolder}/{f}"),
            Is.EquivalentTo(guides), "no guide without its toolset");
    }

    [Test]
    public void TheReposSkillsFolder_HoldsOnlyWhatTheAgentWrites()
    {
        // README.md marks the folder (AgentHost.FindSkillsDirectory); HelloWorld.md is a test fixture.
        string skills = Path.Combine(TestPaths.RepoRoot, "Skills");
        var other = FilesUnder(skills).Where(f => f is not (SkillFiles.FolderMarker or "HelloWorld.md")
            && !f.StartsWith(SkillFiles.FunctionsFolder + "/", StringComparison.Ordinal) && !f.StartsWith("tools/", StringComparison.Ordinal));

        Assert.That(other, Is.Empty, "a built-in prompt goes in louis-agent.core/Prompts/");
    }
}
