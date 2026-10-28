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
        "louis-agent.core", "louis-agent.choices", "louis-agent.session-stream", "identities", "packs", "src", "tools",
        "evals", "tests"
    ];

    /// <summary>
    /// The folders over the limit today, repo-relative with forward slashes, generated from the tree on 2026-10-28;
    /// remove a folder's line in the commit that splits it.
    /// </summary>
    private static readonly HashSet<string> Oversized =
    [
        "evals/louis-agent.evals/Api",
        "evals/louis-agent.evals/Datasets",
        "evals/louis-agent.evals/Protocol",
        "evals/louis-agent.evals/Running",
        "louis-agent.core/Pricing/Prices",
        "louis-agent.core/Tools/Agents/SubSessions",
        "louis-agent.core/Tools/Web/Browsing",
        "louis-agent.core/Tools/Web/Search",
        "louis-agent.core/Usage/Records",
        "packs/louis-agent.pack.shops/Catalogues",
        "src/louis-agent.api/Admin/Replies",
        "src/louis-agent.api/Messages",
        "src/louis-agent.puls/Admin/Grades",
        "src/louis-agent.puls/Admin/Reviews",
        "src/louis-agent.puls/Components/Landing",
        "src/louis-agent.web.shared/Chats",
        "src/louis-agent.web/Git",
        "tests/louis-agent.api.tests",
        "tests/louis-agent.api.tests/Endpoints",
        "tests/louis-agent.api.tests/Sessions",
        "tests/louis-agent.core.tests",
        "tests/louis-agent.core.tests/Agent",
        "tests/louis-agent.core.tests/RepeatedNames/Scanning",
        "tests/louis-agent.evals.puls.tests",
        "tests/louis-agent.evals.puls.tests/Evals/Cook/Grading",
        "tests/louis-agent.evals.tests/Running",
        "tests/louis-agent.puls.tests/Admin/Reviews",
        "tests/louis-agent.testing",
        "tests/louis-agent.web.shared.tests/Chats",
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
