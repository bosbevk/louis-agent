using LouisAgent.Choices;
using LouisAgent.Composition.Prompts;
using LouisAgent.Composition.Packs;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Packs.Rules.Tests;

/// <summary>
/// Every built-in prompt of every pack and every identity in this repository opens with its header: its name (the
/// file's own) and its description, and the level of the folder it is in (ADR 0133, ADR 0063). Read from the disk,
/// each in its project's prompt folders, so a prompt added without one fails here, not when a chat opens.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0063-a-prompt-says-what-it-is.md">ADR 0063: a prompt says what it is</seealso>
public class EveryPromptHasAHeaderTests
{
    /// <summary>The folders whose projects carry prompts of their own: the packs, and the identities.</summary>
    private static IEnumerable<string> ProjectFolders() =>
        new[] { Path.Combine(TestPaths.RepoRoot, "packs/tools"), Path.Combine(TestPaths.RepoRoot, "src/site") }
            .Concat(TestPaths.Demos().Select(demo => Path.Combine(demo, "identity")))
            .Where(Directory.Exists)
            .SelectMany(Directory.EnumerateDirectories);

    /// <summary>
    /// Every prompt, by its path in its project, from the prompt folders its project file embeds (CL68.1), the
    /// folders' READMEs aside.
    /// </summary>
    private static List<(string Path, string Text)> Prompts() =>
    [
        .. ProjectFolders()
            .SelectMany(project => TestPaths.PromptFiles(project)
                .Select(file => (Path: file, Text: File.ReadAllText(Path.Combine(project, file)))))
            .Where(prompt => !prompt.Path.EndsWith("README.md", StringComparison.Ordinal))
    ];

    /// <summary>
    /// Each prompt has a header, which the reader takes: its name is its file's, with its description.
    /// </summary>
    [Test]
    public void EveryBuiltInPrompt_OpensWithAHeader()
    {
        var prompts = Prompts();

        Assert.That(prompts, Has.Count.GreaterThan(20), "the prompts were found");
        Assert.That(prompts.Where(p => PromptHeaders.Split(p.Path, p.Text).Header is null).Select(p => p.Path),
            Is.Empty, "these prompts have no header");
    }

    /// <summary>
    /// An identity's skill is a folder holding its <c>SKILL.md</c>, at <c>agents/{identity}/skills/{name}/</c>
    /// (CL68.1, Louis: "each skill in it's folder, and SKILL.md"), never a loose file in <c>skills/</c>.
    /// </summary>
    [Test]
    public void EverySkill_IsAFolderWithItsSkillFile()
    {
        var skills = Prompts().Select(p => p.Path.Split('/'))
            .Where(parts => parts.Length > 3 && parts[0] == PromptFolder.Agents.Wire()
                                             && string.Join('/', parts).StartsWith(SkillFiles.SkillsOf(parts[1]),
                                                 StringComparison.Ordinal))
            .ToList();
        var loose = skills.Where(parts => parts.Length != 5 || parts[4] != PromptFileName.Skill.Wire())
            .Select(parts => string.Join('/', parts));

        Assert.That(skills, Is.Not.Empty, "the identities' skills were found");
        Assert.That(loose, Is.Empty);
    }

    /// <summary>A role's or mode's header names the level of its folder; a guide's none.</summary>
    [Test]
    public void EachHeader_NamesTheLevelOfItsFolder()
    {
        var wrong = Prompts().Select(p => (p.Path, Level: PromptHeaders.Split(p.Path, p.Text).Header?.Level))
            .Where(p => p.Level != LevelOf(p.Path)).Select(p => $"{p.Path}: {p.Level}");

        Assert.That(wrong, Is.Empty);
    }

    /// <summary>
    /// The level a prompt's folders stand for, the deepest on its path (ADR 0133): an agent's voice is under
    /// <c>agents/</c>, and its subagent's a few folders deeper, under <c>subagents/</c>; none outside them.
    /// </summary>
    private static RoleLevel? LevelOf(string path) =>
        path.Split('/').SkipLast(1).Select(LevelOfFolder).LastOrDefault(level => level is not null);

    /// <summary>The level one folder stands for, or none: a folder's name is parsed once, then switched on.</summary>
    /// <remarks>A switch on the folder, one arm per level, read like a table (ADR 0051, ADR 0043).</remarks>
    private static RoleLevel? LevelOfFolder(string folder) =>
        SettingChoice.TryParse<PromptFolder>(folder, out var known) ? LevelOf(known!.Value) : null;

    /// <summary>The level a known folder stands for, or none.</summary>
    private static RoleLevel? LevelOf(PromptFolder folder) => folder switch
    {
        PromptFolder.Agents => RoleLevel.Agent,
        PromptFolder.Subagents => RoleLevel.Subagent,
        PromptFolder.Minions => RoleLevel.Minion,
        _ => null
    };
}
