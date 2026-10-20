using LouisAgent.Web.Git;

namespace LouisAgent.Web.Tests.Git;

// W2b: a unified diff as the viewer's rows: the hunk header sets the line numbers, context lines count on both sides,
// a removal on the old side, an addition on the new; a binary diff is one note.
public class UnifiedDiffTests
{
    private const string Diff =
        "diff --git a/a.txt b/a.txt\nindex 1..2 100644\n--- a/a.txt\n+++ b/a.txt\n@@ -1,3 +1,3 @@\n one\n-two\n+twee\n three\n\\ No newline at end of file\n";

    [Test]
    public void TheRows_CarryTheirKinds_AndLineNumbers()
    {
        var (rows, added, removed) = UnifiedDiff.Parse(Diff);

        Assert.That(rows.Select(r => r.Kind), Is.EqualTo(new[] { "hunk", "ctx", "del", "add", "ctx", "note" }));
        Assert.That(rows[1], Is.EqualTo(new DiffRow("ctx", "1", "1", "one")));
        Assert.That(rows[2], Is.EqualTo(new DiffRow("del", "2", "", "two")));
        Assert.That(rows[3], Is.EqualTo(new DiffRow("add", "", "2", "twee")));
        Assert.That(rows[4], Is.EqualTo(new DiffRow("ctx", "3", "3", "three")));
        Assert.That((added, removed), Is.EqualTo((1, 1)));
    }

    [Test]
    public void TheFileHeaders_AreNotRows_ButABinaryNoteIs()
    {
        var (rows, added, removed) = UnifiedDiff.Parse("diff --git a/x.png b/x.png\nBinary files a/x.png and b/x.png differ\n");

        Assert.That(rows, Is.EqualTo(new[] { new DiffRow("note", "", "", "Binary files a/x.png and b/x.png differ") }));
        Assert.That((added, removed), Is.EqualTo((0, 0)));
    }

    [Test]
    public void WindowsLineEndings_AreRead() =>
        Assert.That(UnifiedDiff.Parse(Diff.Replace("\n", "\r\n")).Rows, Has.Count.EqualTo(6));
}
