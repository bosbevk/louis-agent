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

    /// <summary>A TODO row: matches <c>| F61 | Accounts, tenants … | Next | [plan](…) |</c> and gives <c>F61</c>.</summary>
    [GeneratedRegex(@"^\| ((?:FX|F|CL)\d+(?:\.\d+)?) \| [^|]+ \| [^|]+ \| [^|]+ \|$", RegexOptions.Multiline)]
    private static partial Regex TodoIds();

    /// <summary>A done row: matches <c>| F61.5 | 2026-11-07 | Passkeys |</c> and gives the id.</summary>
    [GeneratedRegex(@"^\| ((?:FX|F|CL)\d+(?:\.\d+)?) \| \d{4}-\d{2}-\d{2} \|", RegexOptions.Multiline)]
    private static partial Regex DoneRows();

    /// <summary>An open row, under the Open heading: matches <c>| F61 | Accounts, tenants … | Next |</c> (no date) and gives the id.</summary>
    [GeneratedRegex(@"^\| ((?:FX|F|CL)\d+(?:\.\d+)?) \| [^|\d][^|]* \| [^|]+ \|$", RegexOptions.Multiline)]
    private static partial Regex OpenRows();

    /// <summary>A counts row: matches <c>| Fixes (`FX`) | 55 | 0 |</c> and gives the kind, done and open.</summary>
    [GeneratedRegex(@"^\| \w+ \(`(F|FX|CL)`\) \| (\d+) \| (\d+) \|", RegexOptions.Multiline)]
    private static partial Regex Counts();

    private static HashSet<string> Done() => [.. DoneRows().Matches(WorkLog).Select(m => m.Groups[1].Value)];

    private static HashSet<string> Open() => [.. OpenRows().Matches(WorkLog).Select(m => m.Groups[1].Value)];

    private static string Kind(string id) => id.StartsWith("FX") ? "FX" : id.StartsWith("CL") ? "CL" : "F";

    [Test]
    public void EveryFixInTheTable_HasARow()
    {
        var named = FixesIds().Matches(File.ReadAllText(Path.Combine(Root, "src/docs/fixes/README.md")))
            .Select(m => m.Groups[1].Value).Distinct();

        Assert.That(named.Except(Done()), Is.Empty, "add the row to src/docs/work-log.md");
    }

    [Test]
    public void TheOpenList_IsTheTodos()
    {
        var todo = TodoIds().Matches(File.ReadAllText(Path.Combine(Root, "TODO.md")))
            .Select(m => m.Groups[1].Value).ToHashSet();

        // A done feature may still be open for its leftovers (F18, F54), so open and done can share an id.
        Assert.Multiple(() =>
        {
            Assert.That(todo.Except(Open()), Is.Empty, "open in the TODO, not in the work log's Open table");
            Assert.That(Open().Except(todo), Is.Empty, "in the work log's Open table, not in the TODO");
        });
    }

    /// <summary>Every row has a real date, no id is listed twice, and the counts at the top are the rows below.</summary>
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
