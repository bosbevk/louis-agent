using LouisAgent.Core.Tools.Workspace.Listing;

namespace LouisAgent.Core.Tests.Tools.Workspace.Listing;

// S (CL15): the tree ListDirectoryTree draws: directories first, then files, by name; a branch per level, the last
// entry closing it; build output skipped; nothing below the depth asked for.
public class DirectoryTreeTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_root, "src", "deep"));
        Directory.CreateDirectory(Path.Combine(_root, "bin"));
        File.WriteAllText(Path.Combine(_root, "src", "x.cs"), "");
        File.WriteAllText(Path.Combine(_root, "a.txt"), "");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    private static string Lines(params string[] lines) => string.Concat(lines.Select(l => l + Environment.NewLine));

    [Test]
    public void DirectoriesFirst_BranchesPerLevel_BuildOutputSkipped() =>
        Assert.That(DirectoryTree.Draw(_root, maxDepth: 3), Is.EqualTo(Lines(
            "├── 📁 src",
            "│   ├── 📁 deep",
            "│   └── 📄 x.cs",
            "└── 📄 a.txt")));

    [Test]
    public void TheDepth_StopsTheTree() =>
        Assert.That(DirectoryTree.Draw(_root, maxDepth: 1), Is.EqualTo(Lines("├── 📁 src", "└── 📄 a.txt")));
}
