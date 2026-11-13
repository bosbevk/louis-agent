using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests.Docs;

/// <summary>
/// Nothing is finished until what it taught is written down: every done fix has its lesson under its own heading in
/// the lessons file, and every done feature and cleanup is named in a write-up (Louis, 2026-11-13).
/// </summary>
/// <remarks>
/// A ratchet, as <see cref="FileSizeTests"/>. The rule was there in words and about a hundred lessons were still
/// skipped, so the ones owed on the day the test arrived are listed, a new row in the work log with no write-up
/// fails, and a listed one that has its write-up fails too until it is taken off. The lists can only shrink.
/// </remarks>
public partial class WriteUpsTests
{
    private const string LessonHeading = "## FX";

    /// <summary>The done fixes with no lesson yet; none is added, and each leaves when its lesson is written.</summary>
    private static readonly HashSet<string> LessonsOwed =
    [
        "FX38", "FX46", "FX47", "FX48", "FX49", "FX50", "FX51", "FX52", "FX53", "FX54", "FX55", "FX56", "FX57",
        "FX58", "FX62", "FX63", "FX64", "FX65", "FX67", "FX68", "FX69", "FX70", "FX71", "FX72", "FX73", "FX74",
        "FX75", "FX76", "FX77", "FX78", "FX79", "FX80", "FX81", "FX82", "FX84", "FX85", "FX86", "FX87", "FX88",
        "FX89", "FX90", "FX91", "FX92", "FX93", "FX94", "FX95", "FX96", "FX97", "FX98", "FX99", "FX100", "FX101",
        "FX102", "FX103", "FX104", "FX105", "FX106", "FX107", "FX108", "FX109", "FX110", "FX111", "FX112", "FX113",
        "FX114", "FX115", "FX116", "FX117", "FX118", "FX119", "FX120", "FX121", "FX122", "FX123", "FX124", "FX125",
        "FX126", "FX127", "FX128", "FX129", "FX131", "FX132", "FX133", "FX134", "FX135", "FX137", "FX138", "FX139",
        "FX140", "FX141", "FX142", "FX144", "FX145", "FX146", "FX147", "FX149", "FX150", "FX151", "FX152"
    ];

    /// <summary>The done features and cleanups named in no write-up yet; the same rule as the lessons.</summary>
    private static readonly HashSet<string> WriteUpsOwed =
    [
        "CL1", "CL2", "CL4", "CL5", "CL6", "CL7", "CL8", "CL9", "CL10", "CL13", "CL28", "CL30", "CL31", "CL32",
        "CL33", "CL56", "CL57", "CL58", "CL60.1", "CL60.2", "CL60.3", "CL60.4", "CL60.5", "F25", "F26", "F29",
        "F35.8", "F48.2", "F53.1", "F53.2", "F59.9", "F105", "F106.3.1", "F106.3.2", "F110", "F113", "F115", "F116",
        "F117"
    ];

    /// <summary>
    /// An id in running text: finds <c>F61.5</c> in <c>F61.5: a passkey keeps…</c> and <c>CL15</c> in
    /// <c>(CL15).</c>, never the <c>F1</c> inside <c>PDF1</c>.
    /// </summary>
    [GeneratedRegex(@"(?<![A-Za-z0-9])(?:FX|F|CL)\d+(?:\.\d+)*")]
    private static partial Regex Ids();

    /// <summary>A fix's lesson is a paragraph under a heading that starts with the fix's id.</summary>
    [Test]
    public void EveryDoneFix_HasItsLesson_UnlessStillOwed()
    {
        var done = WorkLogTests.Done().Where(id => id.StartsWith("FX", StringComparison.Ordinal)).ToHashSet();
        var taught = LessonHeadings();

        var missing = done.Except(taught).Except(LessonsOwed).Order().ToList();
        var written = LessonsOwed.Intersect(taught).Order().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(missing, Is.Empty,
                "A finished fix has its lesson under '## FXn · …' in src/docs/learning/fixes-lessons.md " +
                "(Finishing a fix, step 5):\n  " + string.Join(" ", missing));
            Assert.That(written, Is.Empty,
                "Has its lesson now: remove from LessonsOwed so the list only shrinks:\n  " +
                string.Join(" ", written));
        });
    }

    /// <summary>
    /// A feature or cleanup is written up when a write-up names it. A parent is written up through its children,
    /// since each child has its own.
    /// </summary>
    [Test]
    public void EveryDoneFeatureAndCleanup_IsNamedInAWriteUp_UnlessStillOwed()
    {
        var done = WorkLogTests.Done().Where(id => !id.StartsWith("FX", StringComparison.Ordinal)).ToHashSet();
        var named = NamedInAWriteUp();
        var writtenUp = done.Where(id => named.Contains(id) || named.Any(n => n.StartsWith(id + '.'))).ToHashSet();

        var missing = done.Except(writtenUp).Except(WriteUpsOwed).Order().ToList();
        var written = WriteUpsOwed.Intersect(writtenUp).Order().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(missing, Is.Empty,
                "A finished feature or cleanup has its section in its skill's write-up in src/docs/learning/ " +
                "(Finishing a feature, step 9):\n  " + string.Join(" ", missing));
            Assert.That(written, Is.Empty,
                "Has its write-up now: remove from WriteUpsOwed so the list only shrinks:\n  " +
                string.Join(" ", written));
        });
    }

    /// <summary>
    /// The fixes the lessons file has a heading for: <c>## FX46 · A photo we may not check</c> gives FX46.
    /// </summary>
    private static HashSet<string> LessonHeadings() =>
    [
        .. File.ReadLines(Path.Combine(TestPaths.RepoRoot, "src/docs/learning/fixes-lessons.md"))
            .Where(line => line.StartsWith(LessonHeading, StringComparison.Ordinal))
            .Select(line => line.Split(' ')[1])
    ];

    /// <summary>
    /// Every id a write-up names. The list of write-ups (the README) is an index, so it doesn't count.
    /// </summary>
    private static HashSet<string> NamedInAWriteUp() =>
    [
        .. LearningFolders()
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.md"))
            .Where(file => Path.GetFileName(file) != "README.md")
            .SelectMany(file => Ids().Matches(File.ReadAllText(file)).Select(m => m.Value))
    ];

    /// <summary>Write-ups live with what they are about: the framework's, the packs' and each demo's.</summary>
    private static IEnumerable<string> LearningFolders()
    {
        string root = TestPaths.RepoRoot;
        var demos = Directory.EnumerateDirectories(Path.Combine(root, "demos"))
            .Select(demo => Path.Combine(demo, "docs"));

        return demos.Append(Path.Combine(root, "src", "docs")).Append(Path.Combine(root, "packs", "docs"))
            .Select(docs => Path.Combine(docs, "learning"))
            .Where(Directory.Exists);
    }
}
