namespace LouisAgent.Core.Tests;

/// <summary>
/// Keeps folders small enough to find your way in: at most six source files each, a component's markup and its
/// code-behind counting as one. A bigger folder gets subfolders by concern (docs/CLAUDE.md, *Separate by concern*).
/// </summary>
/// <remarks>
/// A ratchet, as <see cref="FileSizeTests"/> and over the same roots, tests included: the folders over the limit today
/// are listed in Oversized, a new one fails the test, and a listed folder that has come under it fails too, so the
/// list can only shrink. A listed folder is split when a step next touches it, never in a sweep of its own.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0010-folders-by-concern.md">ADR 0010: folders by concern</seealso>
public class FolderSizeTests
{
    /// <summary>How many source files a folder may hold.</summary>
    private const int Limit = 6;

    /// <summary>Where the source lives: the same roots the file-size rule reads.</summary>
    private static readonly string[] Roots =
    [
        "src", "packs", "packs-evals", "packs-tests", "demos", "tools"
    ];

    /// <summary>
    /// The folders over the limit today, repo-relative with forward slashes, generated from the tree on 2026-10-28;
    /// remove a folder's line in the commit that splits it.
    /// </summary>
    private static readonly HashSet<string> Oversized =
    [
        "src/evals/louis-agent.evals/Api",
        "src/evals/louis-agent.evals/Running",
        "src/engine/louis-agent.core/Pricing/Prices",
        "src/engine/louis-agent.core/Tools/Agents/SubSessions",
        "src/engine/louis-agent.core/Tools/Web/Browsing",
        "src/engine/louis-agent.core/Tools/Web/Search",
        "src/engine/louis-agent.core/Usage/Records",
        "src/hosts/louis-agent.api/Admin/Replies",
        "src/hosts/louis-agent.api/Messages",
        "demos/puls/web/louis-agent.puls/Admin/Grades",
        "demos/puls/web/louis-agent.puls/Admin/Reviews",
        "demos/puls/web/louis-agent.puls/Components/Landing",
        "src/web/louis-agent.web.shared/Chats",
        "demos/louis-agent/web/louis-agent.web/Git",
        "demos/puls/tests/louis-agent.api.tests",
        "demos/puls/tests/louis-agent.api.tests/Endpoints",
        "demos/puls/tests/louis-agent.api.tests/Sessions",
        "src/tests/engine/louis-agent.core.tests",
        "src/tests/engine/louis-agent.core.tests/Agent",
        "src/tests/engine/louis-agent.core.tests/RepeatedNames/Scanning",
        "src/tests/evals/louis-agent.evals.tests/Running",
        "demos/puls/tests/louis-agent.identity.puls.evals.tests/Evals/Checks",
        "demos/puls/tests/louis-agent.puls.tests/Admin/Reviews",
        "src/tests/shared/louis-agent.testing",
        "src/tests/web/louis-agent.web.shared.tests/Chats",
    ];

    /// <summary>
    /// No folder holds more than six source files unless it is still listed, and a listed folder that has come under
    /// the limit is taken off.
    /// </summary>
    [Test]
    public void AFolder_HoldsAtMostSixSourceFiles_UnlessStillOversized()
    {
        var over = Roots.Select(root => Path.Combine(TestPaths.RepoRoot, root))
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories).Prepend(root))
            .Where(folder => !InBuildOutput(folder) && CountSourceFiles(Directory.EnumerateFiles(folder)) > Limit)
            .Select(folder => Path.GetRelativePath(TestPaths.RepoRoot, folder).Replace('\\', '/'))
            .ToHashSet();

        var newOffenders = over.Except(Oversized).Order().ToList();
        var shrunk = Oversized.Except(over).Order().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(newOffenders, Is.Empty,
                $"A folder holds at most {Limit} source files: give it subfolders by concern (docs/CLAUDE.md, "
                + "Separate by concern):\n  " + string.Join("\n  ", newOffenders));
            Assert.That(shrunk, Is.Empty,
                "Within the limit now: remove from Oversized so the list only shrinks:\n  " +
                string.Join("\n  ", shrunk));
        });
    }

    /// <summary>A component's markup and its code-behind are one thing to find, so they count once.</summary>
    [Test]
    public void AComponentAndItsCodeBehind_CountAsOne()
    {
        string[] files = ["Page.razor", "Page.razor.cs", "Panel.razor", "Rows.cs", "notes.md", "site.css"];

        Assert.That(CountSourceFiles(files), Is.EqualTo(3));
    }

    /// <summary>
    /// How many source files a folder holds: its .cs and .razor files, a code-behind not counted beside its markup.
    /// </summary>
    internal static int CountSourceFiles(IEnumerable<string> files)
    {
        var names = files.Select(Path.GetFileName).OfType<string>().ToList();
        var markup = names.Where(n => n.EndsWith(".razor", StringComparison.Ordinal)).ToHashSet();
        return names.Count(n => markup.Contains(n) || (n.EndsWith(".cs", StringComparison.Ordinal) &&
                                                       !markup.Contains(n[..^".cs".Length])));
    }

    /// <summary>What the build wrote (bin/, obj/) isn't ours to count.</summary>
    private static bool InBuildOutput(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Contains("bin") || parts.Contains("obj");
    }
}
