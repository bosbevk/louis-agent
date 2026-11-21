namespace LouisAgent.Core.Tests.Projects;

/// <summary>
/// F166.3: the shared test library finds a repository's root by its solution file, whatever it is called, so a
/// product's tests in a repository of its own find theirs (Louis's pick A of finding 5), and this repository's still
/// finds its own.
/// </summary>
public class TestPathsTests
{
    private string _root = null!;

    /// <summary>A folder of its own for each test, deleted after.</summary>
    [SetUp]
    public void Folder() => _root = Directory.CreateTempSubdirectory("louis-root-").FullName;

    /// <summary>Deletes it, whatever happened.</summary>
    [TearDown]
    public void Tidy() => Directory.Delete(_root, recursive: true);

    /// <summary>A folder deep below a solution file of any name finds that solution's folder.</summary>
    [TestCase("Puls.sln")]
    [TestCase("Any.slnx")]
    public void AFolderBelowASolution_FindsItsFolder(string solution)
    {
        File.WriteAllText(Path.Combine(_root, solution), "");
        string below = Directory.CreateDirectory(Path.Combine(_root, "tests", "Puls.Web.Tests", "bin", "Debug"))
            .FullName;

        Assert.That(TestPaths.RootAbove(below), Is.EqualTo(_root));
    }

    /// <summary>With no solution file anywhere above, it says so, naming where it looked from.</summary>
    [Test]
    public void NoSolutionAbove_IsRefused_NamingTheFolder()
    {
        string below = Directory.CreateDirectory(Path.Combine(_root, "nothing", "here")).FullName;

        var refused = Assert.Throws<InvalidOperationException>(() => TestPaths.RootAbove(below));
        Assert.That(refused.Message, Does.Contain(below));
    }

    /// <summary>This repository's tests still find this repository.</summary>
    [Test]
    public void ThisRepository_IsStillFound() =>
        Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, "louis-agent-solution.sln")), Is.True);
}
