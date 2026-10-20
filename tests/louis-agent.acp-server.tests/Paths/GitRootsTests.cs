using LouisAgent.AcpServer.Paths;

namespace LouisAgent.AcpServer.Tests.Paths;

// S (CL15): the repository a file from the IDE belongs to: the nearest folder above it with a .git, straight from the
// path when it exists here, else through the container mapping; nothing when there is none.
public class GitRootsTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Directory.CreateTempSubdirectory("acp-git-").FullName;
        Directory.CreateDirectory(Path.Combine(_root, "repo", ".git"));
        Directory.CreateDirectory(Path.Combine(_root, "repo", "src", "deep"));
        File.WriteAllText(Path.Combine(_root, "repo", "src", "deep", "A.cs"), "");
        Directory.CreateDirectory(Path.Combine(_root, "loose"));
        File.WriteAllText(Path.Combine(_root, "loose", "B.cs"), "");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    [Test]
    public void TheNearestFolderWithAGit_AboveTheFile() =>
        Assert.That(new GitRoots(new ContainerPaths(() => null, [])).Find(Path.Combine(_root, "repo", "src", "deep", "A.cs")),
            Is.EqualTo(Path.Combine(_root, "repo")));

    [Test]
    public void ThroughTheContainerMapping_WhenThePathIsNotHere() =>
        // A host path that exists on no machine (rooted, so on Linux it never walks up from the test's own folder).
        Assert.That(new GitRoots(new ContainerPaths(() => $"/nonexistent-host/code={_root.Replace('\\', '/')}", [])).Find("/nonexistent-host/code/repo/src/deep/A.cs"),
            Is.EqualTo(Path.GetFullPath(Path.Combine(_root, "repo"))));

    [Test]
    public void NoGitAbove_IsNothing() =>
        Assert.That(new GitRoots(new ContainerPaths(() => null, [])).Find(Path.Combine(_root, "loose", "B.cs")), Is.Empty);
}
