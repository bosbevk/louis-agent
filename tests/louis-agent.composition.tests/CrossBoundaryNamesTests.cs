using LouisAgent.Composition;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Skills.Roles;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Composition.Tests;

// F18 step 7.5.3 (ADR 0043): a name that crosses a boundary stays text there: an identity names its packs, a role file's header lists the toolsets it adds (`toolsets: shops?`).
// The composition root is the one place that sees every identity and pack, so it checks that each such name is one the catalogue knows, and a typo fails here and not when a chat opens.
public class CrossBoundaryNamesTests
{
    private static AgentCatalog Puls() => Plugins.FromSettings("puls", null);

    [Test]
    public void EveryPackAnIdentityNames_IsInTheCatalogue()
    {
        var catalog = Puls();
        var known = catalog.Packs.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var identity in catalog.Identities)
            Assert.That(identity.Packs.Where(p => !known.Contains(p)), Is.Empty, $"{identity.Name} names a pack the catalogue does not have");
    }

    [Test]
    public void EveryToolsetARolesHeaderLists_IsAToolsetTheCatalogueHas()
    {
        var catalog = Puls();
        var known = catalog.Toolsets.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var identity in catalog.Identities)
            Assert.That(Unknown(identity.Prompts, known), Is.Empty, $"{identity.Name}: a role's header lists a toolset no pack or core provides");
    }

    // The check itself, on role files written for the test: a name the catalogue has passes (the optional mark does not matter), a typo is reported with its file.
    [Test]
    public void ARoleHeader_ThatListsATypo_IsReported_AndOneThatListsAKnownNameIsNot()
    {
        var known = new HashSet<string> { CoreToolset.Web.Name(), CoreToolset.Files.Name() };
        var roles = new[]
        {
            ("specialists/good.md", $"---\ntoolsets: {CoreToolset.Web.Name()}, {CoreToolset.Files.Name()}?\n---\nA role."),
            ("specialists/typo.md", $"---\ntoolsets: {CoreToolset.Web.Name()}, fils?\n---\nA role."),
            ("specialists/none.md", "A role with no header."),
        };

        Assert.That(Unknown(roles, known), Is.EqualTo(new[] { "specialists/typo.md: fils" }));
    }

    private static List<string> Unknown(IFileProvider prompts, ISet<string> known) => Unknown(RoleFiles(prompts), known);

    /// <summary>The toolsets (without the optional mark) that role files list in their headers and <paramref name="known"/> lacks, each with its file.</summary>
    private static List<string> Unknown(IEnumerable<(string File, string Text)> roles, ISet<string> known) =>
        [.. roles.SelectMany(r => RoleHeader.Split(r.Text).Toolsets.Select(ToolsetCatalog.NameOf).Where(name => !known.Contains(name)).Select(name => $"{r.File}: {name}"))];

    private static IEnumerable<(string File, string Text)> RoleFiles(IFileProvider prompts) =>
        new[] { RoleCatalog.SpecialistsFolder, RoleCatalog.MinionsFolder, RoleCatalog.AgentFolder }.SelectMany(folder => MarkdownIn(prompts, folder));

    private static IEnumerable<(string File, string Text)> MarkdownIn(IFileProvider prompts, string folder)
    {
        foreach (var entry in prompts.GetDirectoryContents(folder))
        {
            string path = $"{folder}/{entry.Name}";
            if (entry.IsDirectory)
                foreach (var nested in MarkdownIn(prompts, path)) yield return nested;
            else if (entry.Name.EndsWith(".md", StringComparison.Ordinal))
            {
                using var reader = new StreamReader(entry.CreateReadStream());
                yield return (path, reader.ReadToEnd());
            }
        }
    }
}
