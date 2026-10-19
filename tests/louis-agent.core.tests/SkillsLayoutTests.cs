using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Tests;

// docs/CLAUDE.md, "Skill files follow the levels" (CL8, ADR 0017 and 0018): a built-in prompt lives in core's Prompts/,
// in the folder of the level that reads it, or in toolsets/ when every level shares it; the repo's Skills/ holds only
// what the agent writes. Read from the folders, so a misplaced file fails here before a session ever loads it.
public class SkillsLayoutTests
{
    /// <summary>The folders under Prompts/ a built-in file may be in: a level's, its function skills', or the shared guides'.</summary>
    private static readonly string[] PromptFolders =
        [RoleCatalog.AgentFolder, SkillFiles.FunctionsFolder, RoleCatalog.SpecialistsFolder, RoleCatalog.MinionsFolder, SkillFiles.ToolsetsFolder,
         SkillFiles.SharedFolder];

    private static readonly RoleLevel[] Levels = Enum.GetValues<RoleLevel>();

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
                || f.Folder == RoleCatalog.AgentFolder)
            .Select(f => f.File);

        Assert.That(misplaced, Is.Empty, "Prompts/ holds agent/{product}/, agent/functions/, specialists/, minions/ and toolsets/ only");
    }

    [Test]
    public void EveryPrompt_IsEmbedded_AndNothingElseIs()
    {
        Assert.That(SkillFileSource.CoreFiles, Is.EquivalentTo(FilesUnder(TestPaths.Prompts)), "the csproj embeds Prompts/** and only that (CL15: the registered test roles are beside them, not among them)");
    }

    [Test]
    public void ALanguageFile_IsOnlyItsVoice_AndItsProductHasTheSharedPart()
    {
        // CL9 step 6: {product}/voice-{xx}.md is how the product talks in one language; {product}/voice.md holds the rest.
        foreach (string mode in RoleCatalog.Available(RoleLevel.Agent).Where(m => RoleCatalog.LanguageOf(m) is not null))
        {
            string text = SkillFileSource.ReadEmbedded(RoleCatalog.FileName(mode, RoleLevel.Agent))!;
            Assert.That(RoleCatalog.ModeParts(text).Routing, Is.Empty, $"{mode} has no routing of its own");
            Assert.That(SkillFileSource.ReadEmbedded(RoleCatalog.FileName(RoleCatalog.SharedOf(mode), RoleLevel.Agent)), Is.Not.Null,
                $"{mode}'s product has its shared voice.md");
        }
    }

    [Test]
    public void NoRole_IsInTwoLevels()
    {
        // A role name means one file, so PERSONALITIES_ENABLED names roles without a level.
        var twice = Levels.SelectMany(RoleCatalog.Available).GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key);

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
