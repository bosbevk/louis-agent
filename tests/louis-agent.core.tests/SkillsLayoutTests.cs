using LouisAgent.Core.Agent;
using LouisAgent.Core.Skills;

namespace LouisAgent.Core.Tests;

// docs/CLAUDE.md, "Skill files follow the levels" (CL8, ADR 0017 and 0018): a built-in prompt lives in core's Prompts/,
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
        // agent/ holds a folder per product (CL9) beside functions/, and no file of its own.
        var misplaced = FilesUnder(TestPaths.Prompts).Select(f => (File: f, Folder: Path.GetDirectoryName(f)!.Replace('\\', '/')))
            .Where(f => (!PromptFolders.Contains(f.Folder) && !System.Text.RegularExpressions.Regex.IsMatch(f.Folder, "^agent/[a-z0-9-]+$"))
                || f.Folder == Personalities.AgentFolder)
            .Select(f => f.File);

        Assert.That(misplaced, Is.Empty, "Prompts/ holds agent/{product}/, agent/functions/, specialists/, minions/ and toolsets/ only");
    }

    [Test]
    public void EveryPrompt_IsEmbedded_AndNothingElseIs()
    {
        Assert.That(SkillFileSource.EmbeddedFiles, Is.EquivalentTo(FilesUnder(TestPaths.Prompts)), "the csproj embeds Prompts/** and only that");
    }

    [Test]
    public void AProductsRouting_IsTheSameInEachOfItsModes()
    {
        // CL9: a mode is one file, so a product's routing is written in each of its modes; they must not drift apart.
        foreach (var product in Personalities.Available(SkillLevel.Agent).GroupBy(n => n.Split('/')[0]))
        {
            var routings = product.Select(n => Personalities.ModeParts(SkillFileSource.ReadEmbedded(Personalities.FileName(n, SkillLevel.Agent))!).Routing)
                .Where(r => r.Length > 0).Select(r => r.ReplaceLineEndings("\n")).Distinct().ToList();
            Assert.That(routings, Has.Count.LessThanOrEqualTo(1), $"{product.Key}'s modes route differently");
        }
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
