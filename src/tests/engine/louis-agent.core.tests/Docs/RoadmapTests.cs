using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests.Docs;

/// <summary>
/// The roadmap (<c>ROADMAP.md</c>) names its phases' work by id, and every id it names is a row of the TODO (open)
/// or of the work log (done), so the roadmap can't point at work that exists nowhere else.
/// </summary>
public partial class RoadmapTests
{
    private static readonly string Root = TestPaths.RepoRoot;

    /// <summary>A phase row's ids, the last cell: matches <c>| 7 | … | Next | FX4, F41, F42 |</c> and gives the cell.</summary>
    [GeneratedRegex(@"^\| [^|]+ \| [^|]+ \| [^|]+ \| [^|]+ \| ([^|]+) \|$", RegexOptions.Multiline)]
    private static partial Regex PhaseRows();

    /// <summary>An id: <c>F49</c>, <c>FX4</c>, <c>F61.4</c>, <c>CL17</c>.</summary>
    [GeneratedRegex(@"\b(?:FX|F|CL)\d+(?:\.\d+)?\b")]
    private static partial Regex Ids();

    /// <summary>A row of the TODO or the work log that starts with an id.</summary>
    [GeneratedRegex(@"^\| ((?:FX|F|CL)\d+(?:\.\d+)?) \|", RegexOptions.Multiline)]
    private static partial Regex Rows();

    [Test]
    public void EveryIdThePhasesName_IsInTheTodoOrTheWorkLog()
    {
        string roadmap = File.ReadAllText(Path.Combine(Root, "ROADMAP.md"));
        var named = PhaseRows().Matches(roadmap)
            .SelectMany(m => Ids().Matches(m.Groups[1].Value).Select(id => id.Value))
            .Distinct().ToList();
        var known = Rows().Matches(File.ReadAllText(Path.Combine(Root, "TODO.md")))
            .Concat(Rows().Matches(File.ReadAllText(Path.Combine(Root, "src/docs/work-log.md"))))
            .Select(m => m.Groups[1].Value).ToHashSet();

        Assert.That(named, Is.Not.Empty, "the phases table names ids");
        Assert.That(named.Except(known), Is.Empty, "an id the roadmap names is a row of the TODO or the work log");
    }
}
