using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Prompts;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Prompting;
using LouisAgent.Core.Prompting.Headers;
using LouisAgent.Core.Prompting.Roles;

namespace LouisAgent.Core.Tests;

// src/docs/CLAUDE.md, "Skill files follow the levels" (CL8, ADR 0017 and 0018): a built-in prompt lives in one of
// core's prompt folders, the folder of the level that reads it, or toolsets/ when every level shares it; the repo's
// Skills/ holds only what the agent writes. Read from the folders, so a misplaced file fails here before a session ever
// loads it.
public class SkillsLayoutTests
{
    /// <summary>The folder a built-in file may be in directly: what every agent shares.</summary>
    private static readonly string[] PromptFolders = [PromptFolder.Shared.Wire()];

    private static readonly RoleLevel[] Levels = Enum.GetValues<RoleLevel>();

    private static IEnumerable<string> FilesUnder(string root) =>
        Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(root, f).Replace('\\', '/'))
            : [];

    [Test]
    public void EveryPrompt_IsInAKnownFolder()
    {
        // agents/ holds a folder per product (CL9) and no file of its own; skills/ a folder per built-in skill.
        var misplaced = TestPaths.PromptFiles(TestPaths.Prompts)
            .Select(f => (File: f, Folder: Path.GetDirectoryName(f)!.Replace('\\', '/')))
            .Where(f => !PromptFolders.Contains(f.Folder) &&
                        !System.Text.RegularExpressions.Regex.IsMatch(f.Folder,
                            $"^({PromptFolder.Agents.Wire()}|{PromptFolder.Skills.Wire()})/[a-z0-9-]+$"))
            .Select(f => f.File);

        Assert.That(misplaced, Is.Empty, "core's prompts are agents/{product}/, skills/{name}/ and shared/ only");
    }

    [Test]
    public void EveryPrompt_IsEmbedded_AndNothingElseIs()
    {
        Assert.That(SkillFileSource.CoreFiles, Is.EquivalentTo(TestPaths.PromptFiles(TestPaths.Prompts)),
            "the csproj embeds its prompt folders and only those " +
            "(CL15: the registered test roles are beside them, not among them)");
    }

    [Test]
    public void ALanguageFile_IsOnlyItsVoice_AndItsProductHasTheSharedPart()
    {
        // CL9 step 6: {product}/voice-{xx}.md is how the product talks in one language; {product}/voice.md holds the
        // rest.
        foreach (string mode in RoleCatalog.Available(RoleLevel.Agent)
                     .Where(m => RoleCatalog.LanguageOf(m) is not null))
        {
            string file = RoleCatalog.FileName(mode, RoleLevel.Agent);
            string text = PromptHeaders.Split(file, SkillFileSource.ReadEmbedded(file)!).Body;
            Assert.That(RoleCatalog.ModeParts(text).Routing, Is.Empty, $"{mode} has no routing of its own");
            Assert.That(SkillFileSource.ReadEmbedded(RoleCatalog.FileName(RoleCatalog.SharedOf(mode), RoleLevel.Agent)),
                Is.Not.Null,
                $"{mode}'s product has its shared voice.md");
        }
    }

    [Test]
    public void NoRole_IsInTwoLevels()
    {
        // A role name means one file, so PERSONALITIES_ENABLED names roles without a level.
        var twice = Levels.SelectMany(RoleCatalog.Available).GroupBy(n => n).Where(g => g.Count() > 1)
            .Select(g => g.Key);

        Assert.That(twice, Is.Empty);
    }

    /// <summary>
    /// F97.1 step 15: every toolset names the built-in skill it always loads, by its name, and each is a folder in
    /// <c>skills/</c> holding its <c>SKILL.md</c>; no built-in skill is left without its toolset.
    /// </summary>
    [Test]
    public void EveryToolset_NamesItsSkill_InSkills_AndEverySkillItsToolset()
    {
        var names = TestTools.Catalog.Toolsets.Select(t => t.Skill).ToList();

        Assert.That(names, Has.None.Contain("/"), "a toolset names its skill, not a path");
        Assert.That(SkillFileSource.EmbeddedFiles.Where(f => f.StartsWith(PromptFolder.Skills.Wire() + "/")),
            Is.EquivalentTo(names.Select(name => PackToolset.SkillFile(name!))), "no skill without its toolset");
    }

    /// <summary>What the running agent writes belongs to its demo, and only that sits in the demo's folder.</summary>
    [TestCase("demos/louis-agent/skills")]
    public void ADemosSkillsFolder_HoldsOnlyWhatTheAgentWrites(string folder)
    {
        // README.md marks the folder (AgentHost.FindSkillsDirectory).
        string skills = Path.Combine(TestPaths.RepoRoot, folder);
        // What the agent writes: a skill's folder, with its SKILL.md, or a draft in drafts/; tools/ is the old
        // agent-built tools', which step 18 of F97.1 empties.
        var other = FilesUnder(skills).Where(f => f != SkillFiles.FolderMarker
                                                  && !f.StartsWith(SkillFiles.DraftsFolder + "/",
                                                      StringComparison.Ordinal)
                                                  && !IsInASkillsFolder(skills, f)
                                                  && !f.StartsWith("tools/", StringComparison.Ordinal));

        Assert.That(other, Is.Empty, "a built-in prompt goes in src/engine/LouisAgent.Core/'s prompt folders");
    }

    /// <summary>Whether a file sits in a skill's own folder, the one at the top that holds a SKILL.md.</summary>
    private static bool IsInASkillsFolder(string skills, string file) =>
        file.IndexOf('/') is > 0 and var slash && File.Exists(Path.Combine(skills, file[..slash], "SKILL.md"));
}
