using LouisAgent.Composition.Packs;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Packs.Rules.Tests;

/// <summary>
/// Every built-in prompt of every pack and every identity in this repository opens with its header: its name (the
/// file's own) and its description, and the level of the folder it is in (ADR 0017, ADR 0063). Read from the disk,
/// each under its project's <c>prompts/</c>, so a prompt added without one fails here, not when a chat opens.
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

    /// <summary>Every prompt, by its path under its project's <c>prompts/</c>, the folders' READMEs aside.</summary>
    private static List<(string Path, string Text)> Prompts() =>
    [
        .. ProjectFolders()
            .Select(project => Path.Combine(project, EmbeddedPrompts.Folder))
            .Where(Directory.Exists)
            .SelectMany(prompts => Directory.EnumerateFiles(prompts, "*.md", SearchOption.AllDirectories)
                .Select(file => (Path: Path.GetRelativePath(prompts, file).Replace('\\', '/'),
                    Text: File.ReadAllText(file))))
            .Where(prompt => !prompt.Path.EndsWith("README.md", StringComparison.Ordinal))
    ];

    /// <summary>Each prompt has a header, which the reader takes: its name is its file's, with its description.</summary>
    [Test]
    public void EveryBuiltInPrompt_OpensWithAHeader()
    {
        var prompts = Prompts();

        Assert.That(prompts, Has.Count.GreaterThan(20), "the prompts were found");
        Assert.That(prompts.Where(p => PromptHeaders.Split(p.Path, p.Text).Header is null).Select(p => p.Path),
            Is.Empty, "these prompts have no header");
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
    /// The level a prompt's folder stands for (ADR 0017), the first of the three on its path, since an identity with
    /// several agents keeps each one's prompts a folder deeper (the showroom's businesses); none outside the three.
    /// </summary>
    private static RoleLevel? LevelOf(string path) =>
        path.Split('/').SkipLast(1).Select(LevelOfFolder).FirstOrDefault(level => level is not null);

    /// <summary>The level one folder stands for, or none.</summary>
    /// <remarks>A switch on the folder, one arm per level, read like a table (ADR 0051).</remarks>
    private static RoleLevel? LevelOfFolder(string folder) => folder switch
    {
        RoleCatalog.AgentFolder => RoleLevel.Agent,
        RoleCatalog.SpecialistsFolder => RoleLevel.Specialist,
        RoleCatalog.MinionsFolder => RoleLevel.Minion,
        _ => null
    };
}
