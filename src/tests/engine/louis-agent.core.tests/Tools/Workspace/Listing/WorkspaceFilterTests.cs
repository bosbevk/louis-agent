using LouisAgent.Core.Tools.Workspace.Listing;

namespace LouisAgent.Core.Tests.Tools.Workspace.Listing;

// S (CL15): what the listing and search tools skip: build output and tool folders by name, directories only, and what
// the text search reads, by extension.
public class WorkspaceFilterTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_root, "bin"));
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        File.WriteAllText(Path.Combine(_root, "obj"), "a file named like build output");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    [Test]
    public void ADirectoryNamedLikeBuildOutput_IsExcluded_AFileIsNot()
    {
        Assert.That(WorkspaceFilter.IsExcluded(Path.Combine(_root, "bin")), Is.True);
        Assert.That(WorkspaceFilter.IsExcluded(Path.Combine(_root, "src")), Is.False);
        Assert.That(WorkspaceFilter.IsExcluded(Path.Combine(_root, "obj")), Is.False, "only directories are excluded by name");
    }

    [Test]
    public void TheExcludedNames_IgnoreCase() =>
        Assert.That(new[] { "BIN", ".git", "node_modules" }.All(WorkspaceFilter.ExcludedDirectories.Contains), Is.True);

    [TestCase("a.cs", true)]
    [TestCase("a.MD", true)]
    [TestCase("a.png", false)]
    [TestCase("a", false)]
    public void OnlySourceAndDocumentationFiles_AreSearched(string name, bool searchable) =>
        Assert.That(WorkspaceFilter.IsSearchable(name), Is.EqualTo(searchable));
}
