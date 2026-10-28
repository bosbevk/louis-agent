using System.Text.RegularExpressions;

namespace LouisAgent.Web.Shared.Tests;

/// <summary>
/// The portal shows every eval the same way, from the standard records (F18 step 4.5): no view reads a conversation's
/// scores (an eval's own keys) or tests a task's name. And the admin's grades code names nothing of Puls, so the admin
/// can become a package by a folder move.
/// </summary>
/// <remarks>
/// A ratchet, as <see cref="RazorHasNoCodeTests"/> is: the files that still read scores today are listed in
/// <see cref="ReadsScores"/>, a new one fails the test, and a listed file that no longer does fails too, so the list
/// only shrinks; step 7c empties it when the scores split into grades and tracked metrics.
/// </remarks>
[TestFixture]
public class PortalReadsNoScoreKeyTests
{
    /// <summary>A read of a conversation's scores, or a test on a task's name.</summary>
    private static readonly Regex ScoreRead = new("\"scores\"|\\.Scores\\b|Task\\.StartsWith\\(|Task == \"",
        RegexOptions.CultureInvariant);

    /// <summary>Anything of Puls: its texts, their namespace, or its brand components.</summary>
    private static readonly Regex NamesPuls = new("PulsText|LouisAgent\\.Puls\\.Text|Components\\.Brand",
        RegexOptions.CultureInvariant);

    private static readonly string[] Scanned =
    [
        "src/louis-agent.puls/Pages/Admin",
        "src/louis-agent.puls/Components/Admin",
        "src/louis-agent.puls/Admin",
        "src/louis-agent.api/Benchmarks/Reports",
    ];

    private static readonly string[] BuiltToMove =
    [
        "src/louis-agent.puls/Admin/Grades",
        "src/louis-agent.puls/Components/Admin/Grades",
    ];

    /// <summary>
    /// Repo-relative, forward slashes; remove a file's line in the commit that stops it reading scores.
    /// </summary>
    private static readonly HashSet<string> ReadsScores =
    [
        // The run explorer's score reads, moved out of the explorer with its rebuild (step 7b (ii)): the scores kept on
        // each conversation, and the one class that reads their keys.
        "src/louis-agent.puls/Admin/Explorer/Tree/ConversationNode.cs",
        "src/louis-agent.puls/Admin/Scoring/ExplorerScores.cs",
        // The Benchmarks page's score chips, moved out of the page with the live run's table (step 7b (ii)).
        "src/louis-agent.puls/Admin/LiveRuns/LiveTurns.cs",
        "src/louis-agent.api/Benchmarks/Reports/BenchmarkTrends.cs",
    ];

    /// <summary>
    /// No admin view reads an eval's own score keys unless it is still listed, and the list only shrinks.
    /// </summary>
    [Test]
    public void AnAdminView_ReadsNoScoreKey_UnlessStillListed()
    {
        var reading = Scanned.SelectMany(Files).Where(f => ScoreRead.IsMatch(File.ReadAllText(f))).Select(Relative)
            .ToHashSet();

        var newOffenders = reading.Except(ReadsScores).Order().ToList();
        var done = ReadsScores.Except(reading).Order().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(newOffenders, Is.Empty,
                "A view reads only the standard records (the run's graders, a conversation's grades and outcome), never an eval's score key:\n  "
                + string.Join("\n  ", newOffenders));
            Assert.That(done, Is.Empty,
                "Reads no scores any more: remove it, so the list only shrinks:\n  " + string.Join("\n  ", done));
        });
    }

    /// <summary>The admin's grades code names nothing of Puls, so it can move out as a folder.</summary>
    [Test]
    public void TheGradesFolders_NameNothingOfPuls()
    {
        var naming = BuiltToMove.SelectMany(Files).Where(f => NamesPuls.IsMatch(File.ReadAllText(f))).Select(Relative)
            .Order().ToList();

        Assert.That(naming, Is.Empty,
            "The admin's grades code names nothing of Puls (no PulsText, no Components.Brand), so the admin can move out as a folder:\n  "
            + string.Join("\n  ", naming));
    }

    /// <summary>
    /// The C# and Razor files in a folder. A folder that isn't there fails, never scans as empty: step 7b renames the
    /// API's <c>Benchmarks/</c>, and this list must follow it.
    /// </summary>
    private static IEnumerable<string> Files(string folder)
    {
        string dir = Path.Combine(TestPaths.RepoRoot, folder.Replace('/', Path.DirectorySeparatorChar));
        Assert.That(Directory.Exists(dir), Is.True,
            $"No folder {folder}: it was moved or renamed, so update this test's list.");
        return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            .Where(f => (f.EndsWith(".cs") || f.EndsWith(".razor"))
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"));
    }

    /// <summary>A file's path from the repo's root, with forward slashes, as the lists write it.</summary>
    private static string Relative(string file) => Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/');
}
