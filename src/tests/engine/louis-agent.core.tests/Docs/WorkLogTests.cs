using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests.Docs;

/// <summary>
/// The work log (<c>src/docs/work-log.md</c>) is the record of what was finished: every fix in the fixes table has a
/// row, its open list is the TODO's, nothing is both open and done, every row is dated, and the counts at the top
/// are the rows below, so the table Louis reads for "when and what" can't fall behind.
/// </summary>
public partial class WorkLogTests
{
    private static readonly string Root = TestPaths.RepoRoot;

    private static string WorkLog => File.ReadAllText(Path.Combine(Root, "src/docs/work-log.md"));

    /// <summary>A fixes-table row's id: matches <c>| FX46 | A photo …</c>.</summary>
    [GeneratedRegex(@"^\| (FX\d+) \|", RegexOptions.Multiline)]
    private static partial Regex FixesIds();

    /// <summary>
    /// A TODO row: matches <c>| F61 | Accounts, tenants … | Next | [plan](…) |</c> and gives <c>F61</c>.
    /// </summary>
    [GeneratedRegex(@"^\| ((?:FX|F|CL)\d+(?:\.\d+)*) \| [^|]+ \| [^|]+ \| [^|]+ \|\r?$", RegexOptions.Multiline)]
    private static partial Regex TodoIds();

    /// <summary>
    /// A done row: matches <c>| F61.5 | 2026-11-07 | Passkeys |</c> and gives the id; a child of a child
    /// (<c>F106.3.1</c>) counts too.
    /// </summary>
    [GeneratedRegex(@"^\| ((?:FX|F|CL)\d+(?:\.\d+)*) \| \d{4}-\d{2}-\d{2} \|", RegexOptions.Multiline)]
    private static partial Regex DoneRows();

    /// <summary>
    /// An open row, under the Open heading: matches <c>| F61 | Accounts, tenants … | Next |</c> (no date) and gives
    /// the id.
    /// </summary>
    [GeneratedRegex(@"^\| ((?:FX|F|CL)\d+(?:\.\d+)*) \| [^|\d][^|]* \| [^|]+ \|\r?$", RegexOptions.Multiline)]
    private static partial Regex OpenRows();

    /// <summary>A counts row: matches <c>| Fixes (`FX`) | 55 | 0 |</c> and gives the kind, done and open.</summary>
    [GeneratedRegex(@"^\| \w+ \(`(F|FX|CL)`\) \| (\d+) \| (\d+) \|", RegexOptions.Multiline)]
    private static partial Regex Counts();

    /// <summary>Everything the work log says is finished, by id; the write-ups' test reads it too.</summary>
    internal static HashSet<string> Done() => [.. DoneRows().Matches(WorkLog).Select(m => m.Groups[1].Value)];

    /// <summary>Everything the work log still lists as open, by id.</summary>
    private static HashSet<string> Open() => [.. OpenRows().Matches(WorkLog).Select(m => m.Groups[1].Value)];

    /// <summary>Which of the three counts an id belongs to: a feature, a fix or a cleanup.</summary>
    private static string Kind(string id) => id.StartsWith("FX") ? "FX" : id.StartsWith("CL") ? "CL" : "F";

    /// <summary>A fix can't be in the fixes table without its dated row here.</summary>
    [Test]
    public void EveryFixInTheTable_HasARow()
    {
        var named = FixesIds().Matches(File.ReadAllText(Path.Combine(Root, "src/docs/fixes/README.md")))
            .Select(m => m.Groups[1].Value).Distinct();

        Assert.That(named.Except(Done()), Is.Empty, "add the row to src/docs/work-log.md");
    }

    /// <summary>
    /// The TODO's rows are the build order (Louis, 2026-11-07: the framework first, then the agents), so the work
    /// log's open table holds the same ids in the same order, not only the same set.
    /// </summary>
    [Test]
    public void TheOpenList_IsTheTodos_InItsOrder()
    {
        var todo = TodoIds().Matches(File.ReadAllText(Path.Combine(Root, "TODO.md")))
            .Select(m => m.Groups[1].Value).ToList();
        var open = OpenRows().Matches(WorkLog).Select(m => m.Groups[1].Value).ToList();

        // A done feature may still be open for its leftovers (F18, F54), so open and done can share an id.
        Assert.That(open, Is.EqualTo(todo), "the work log's Open table is the TODO's rows, in the TODO's order");
    }

    /// <summary>
    /// Every row has a real date, no id is listed twice, and the counts at the top are the rows below.
    /// </summary>
    [Test]
    public void TheCounts_AreTheRows()
    {
        var done = DoneRows().Matches(WorkLog).Select(m => m.Groups[1].Value).ToList();
        var counts = Counts().Matches(WorkLog)
            .ToDictionary(m => m.Groups[1].Value,
                m => (Done: int.Parse(m.Groups[2].Value), Open: int.Parse(m.Groups[3].Value)));

        Assert.That(WorkLog, Does.Not.Contain("| — |"), "every row is dated");
        Assert.That(done, Is.Unique);
        Assert.Multiple(() =>
        {
            foreach (string kind in new[] { "F", "FX", "CL" })
            {
                Assert.That(counts[kind].Done, Is.EqualTo(done.Count(id => Kind(id) == kind)), $"{kind} done");
                Assert.That(counts[kind].Open, Is.EqualTo(Open().Count(id => Kind(id) == kind)), $"{kind} open");
            }
        });
    }
}
