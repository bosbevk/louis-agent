using LouisAgent.Choices;
using LouisAgent.Composition.Prompts;

namespace LouisAgent.Core.Tests.Composition.Prompts;

/// <summary>
/// Where a prompt sits is one set of names every project shares (ADR 0043, CL68.2): each folder a project embeds its
/// prompts from is a <see cref="PromptFolder"/>, and each file a folder is known by is a <see cref="PromptFileName"/>.
/// </summary>
public class PromptLayoutTests
{
    /// <summary>The projects that embed prompts of their own: core, its tests, every identity and every pack.</summary>
    private static IEnumerable<string> Projects() =>
        new[] { TestPaths.Prompts, Path.Combine(TestPaths.RepoRoot, "src/tests/engine/LouisAgent.Core.Tests") }
            .Concat(new[] { "packs/tools", "src/site" }
                .Select(folder => Path.Combine(TestPaths.RepoRoot, folder))
                .Concat(TestPaths.Demos().Select(demo => Path.Combine(demo, "identity")))
                .Where(Directory.Exists)
                .SelectMany(Directory.EnumerateDirectories));

    /// <summary>Every folder a project's file embeds as prompts is one of the shared names.</summary>
    [Test]
    public void EveryEmbeddedFolder_IsAPromptFolder()
    {
        var folders = Projects().SelectMany(TestPaths.PromptFolders).Distinct().ToList();

        Assert.That(folders, Is.Not.Empty);
        Assert.That(folders.Where(folder => !SettingChoice.TryParse<PromptFolder>(folder, out _)), Is.Empty);
    }

    /// <summary>A known file's wire name is the file's own name, as a folder holds it.</summary>
    [Test]
    public void AKnownFile_IsNamedAsItsFolderHoldsIt() =>
        Assert.That(SettingChoice.Names<PromptFileName>(), Is.EqualTo(new[] { "voice.md", "SKILL.md" }));
}
