namespace LouisAgent.Puls.Tests.Admin;

/// <summary>
/// Counts the admin portal's code that still picks the API's replies apart by name as <c>JsonElement</c>, so the list
/// can only shrink until every reader takes the contract's record. Step 7c is done only when the list is empty.
/// </summary>
/// <remarks>
/// A ratchet, as <c>FileSizeTests</c>: the files that read <c>JsonElement</c> today are listed, a new one fails, and a
/// listed one that no longer reads it fails too, so the list can only shrink. A reader moves to its contract record
/// when 7c rewrites it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public class PortalReadsJsonTests
{
    /// <summary>The portal's folders: its plain classes, its components and its pages.</summary>
    private static readonly string[] Folders =
    [
        "src/louis-agent.puls/Admin",
        "src/louis-agent.puls/Components/Admin",
        "src/louis-agent.puls/Pages/Admin"
    ];

    /// <summary>
    /// The files that read <c>JsonElement</c> today, repo-relative with forward slashes, generated from the tree on
    /// 2026-10-28; remove a file's line in the commit that moves it to its record.
    /// </summary>
    private static readonly HashSet<string> ReadsJson =
    [
        "src/louis-agent.puls/Admin/Explorer/Tree/ConversationNode.cs",
        "src/louis-agent.puls/Admin/Scoring/ExplorerScores.cs",
        "src/louis-agent.puls/Admin/Scoring/ScoreChips.cs",
        "src/louis-agent.puls/Admin/Summary/CategoryTrendReader.cs",
        "src/louis-agent.puls/Components/Admin/BenchmarksPage/Checks/CheckCard.razor.cs",
        "src/louis-agent.puls/Components/Admin/BenchmarksPage/Checks/ChecksList.razor.cs",
        "src/louis-agent.puls/Components/Admin/RunExplorer.razor.cs",
        "src/louis-agent.puls/Components/Admin/Summary/CategoryTrends.razor.cs",
        "src/louis-agent.puls/Pages/Admin/BenchmarkRun.razor.cs",
        "src/louis-agent.puls/Pages/Admin/Benchmarks.razor.cs",
    ];

    /// <summary>
    /// No portal file reads the API's replies as <c>JsonElement</c> unless it is still listed, and a listed one that
    /// has moved to its record is taken off.
    /// </summary>
    [Test]
    public void APortalFile_ReadsTheContractsRecords_UnlessStillListed()
    {
        var reading = Folders.Select(folder => Path.Combine(TestPaths.RepoRoot, folder))
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) ||
                           file.EndsWith(".razor", StringComparison.Ordinal))
            .Where(file => File.ReadAllText(file).Contains("JsonElement", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/'))
            .ToHashSet();

        var newReaders = reading.Except(ReadsJson).Order().ToList();
        var moved = ReadsJson.Except(reading).Order().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(newReaders, Is.Empty,
                "A portal file reads the contract's record, never a JsonElement picked apart by name (ADR 0045):\n  "
                + string.Join("\n  ", newReaders));
            Assert.That(moved, Is.Empty,
                "Reads its record now: remove from ReadsJson so the list only shrinks:\n  " +
                string.Join("\n  ", moved));
        });
    }
}
