using LouisAgent.Choices;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Core.Skills.Roles;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Composition.Tests;

/// <summary>
/// CL27: every built-in prompt, core's and every identity's and pack's, opens with its header: the Agent Skills spec's
/// name (the file's own) and description, and the level of the folder it is in (ADR 0017). The composition root is the
/// one project that sees them all, so a prompt added without one fails here, not when a chat opens.
/// </summary>
public class EveryPromptHasAHeaderTests
{
    /// <summary>Every prompt the hosts can load, by its path under <c>Prompts/</c>, the folders' READMEs aside.</summary>
    private static IEnumerable<(string Path, string Text)> Prompts()
    {
        var everything = Plugins.FromSettings(
            string.Join(",", Enum.GetValues<IdentityKind>().Select(k => k.Wire())),
            string.Join(",", Enum.GetValues<PackKind>().Select(k => k.Wire())));
        return new[] { SkillFileSource.Providers[0] }.Concat(everything.Prompts)
            .SelectMany(provider => MarkdownIn(provider, ""))
            .Where(prompt => !prompt.Path.EndsWith("README.md", StringComparison.Ordinal));
    }

    /// <summary>Each prompt has a header, which the reader takes: its name is its file's, its description there.</summary>
    [Test]
    public void EveryBuiltInPrompt_OpensWithAHeader()
    {
        var prompts = Prompts().ToList();

        Assert.That(prompts, Has.Count.GreaterThan(20), "the prompts were found");
        Assert.That(prompts.Where(p => PromptHeaders.Split(p.Path, p.Text).Header is null).Select(p => p.Path),
            Is.Empty, "these prompts have no header");
    }

    /// <summary>A role's or mode's header names the level of its folder; a guide's and the principles' none.</summary>
    [Test]
    public void EachHeader_NamesTheLevelOfItsFolder()
    {
        var wrong = Prompts().Select(p => (p.Path, Level: PromptHeaders.Split(p.Path, p.Text).Header?.Level))
            .Where(p => p.Level != LevelOf(p.Path)).Select(p => $"{p.Path}: {p.Level}");

        Assert.That(wrong, Is.Empty);
    }

    /// <summary>The level a prompt's folder stands for (ADR 0017); none outside the three.</summary>
    private static RoleLevel? LevelOf(string path) => path.Split('/')[0] switch
    {
        RoleCatalog.AgentFolder => RoleLevel.Agent,
        RoleCatalog.SpecialistsFolder => RoleLevel.Specialist,
        RoleCatalog.MinionsFolder => RoleLevel.Minion,
        _ => null
    };

    /// <summary>Every markdown file under the folder, with its path and text.</summary>
    private static IEnumerable<(string Path, string Text)> MarkdownIn(IFileProvider prompts, string folder)
    {
        foreach (var entry in prompts.GetDirectoryContents(folder.Length == 0 ? "/" : folder))
        {
            string path = folder.Length == 0 ? entry.Name : $"{folder}/{entry.Name}";
            if (entry.IsDirectory)
                foreach (var nested in MarkdownIn(prompts, path))
                    yield return nested;
            else if (entry.Name.EndsWith(".md", StringComparison.Ordinal))
            {
                using var reader = new StreamReader(entry.CreateReadStream());
                yield return (path, reader.ReadToEnd());
            }
        }
    }
}
