using LouisAgent.Web.Git;

namespace LouisAgent.Web.Tests.Git;

// W2b: a multi-file patch (a commit, a branch) split into one section per file, each with its path, its status (new,
// deleted, renamed) and its parsed hunks.
public class PatchTests
{
    private const string TwoFiles =
        "diff --git a/src/x.cs b/src/x.cs\nnew file mode 100644\n--- /dev/null\n+++ b/src/x.cs\n@@ -0,0 +1,2 @@\n+a\n+b\n"
        + "diff --git a/y.cs b/y.cs\nindex 1..2 100644\n--- a/y.cs\n+++ b/y.cs\n@@ -1 +1 @@\n-c\n+d\n";

    [Test]
    public void EachFile_IsASection_WithItsPathStatusAndCounts()
    {
        var files = Patch.Split(TwoFiles);

        Assert.That(files.Select(f => f.Path), Is.EqualTo(new[] { "src/x.cs", "y.cs" }));
        Assert.That(files[0].Status, Is.EqualTo("new file"));
        Assert.That((files[0].Added, files[0].Removed), Is.EqualTo((2, 0)));
        Assert.That(files[1].Status, Is.Null);
        Assert.That((files[1].Added, files[1].Removed), Is.EqualTo((1, 1)));
        Assert.That(files[1].Rows.Select(r => r.Kind), Is.EqualTo(new[] { "hunk", "del", "add" }));
    }

    [Test]
    public void ADeletedOrRenamedFile_SaysSo()
    {
        var files = Patch.Split("diff --git a/old.cs b/old.cs\ndeleted file mode 100644\n" + "diff --git a/a.cs b/b.cs\nrename from a.cs\nrename to b.cs\n");

        Assert.That(files.Select(f => f.Status), Is.EqualTo(new[] { "deleted", "renamed" }));
        Assert.That(files[1].Path, Is.EqualTo("b.cs"));
    }

    [Test]
    public void NoPatch_IsNoFiles() =>
        Assert.That(Patch.Split(""), Is.Empty);
}
