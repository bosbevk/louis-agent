using LouisAgent.Web.Testing.Sites;

namespace LouisAgent.Web.Tests.Testing;

/// <summary>
/// F166.3: a site's project is found wherever its repository keeps it, so a site in a repository of its own (Puls
/// keeps its own under src/) tests its look the way louis-agent's sites do (Louis's pick A of finding 8).
/// </summary>
public class SiteProjectTests
{
    private string _root = null!;

    /// <summary>A repository of its own for each test.</summary>
    [SetUp]
    public void Repository() => _root = Directory.CreateTempSubdirectory("louis-site-project-").FullName;

    /// <summary>Deletes it, whatever happened.</summary>
    [TearDown]
    public void Tidy() => Directory.Delete(_root, recursive: true);

    /// <summary>A project at any depth is found, and a copy in build output is never taken for it.</summary>
    [Test]
    public void AProject_IsFoundAnywhereInItsRepository_ButNeverInBuildOutput()
    {
        string site = Directory.CreateDirectory(Path.Combine(_root, "src", "Shop.Web")).FullName;
        Directory.CreateDirectory(Path.Combine(_root, "src", "Shop.Web", "bin", "Debug", "Shop.Web"));
        Directory.CreateDirectory(Path.Combine(_root, "tests", "Shop.Web.Tests", "obj", "Shop.Web"));

        Assert.That(SiteProject.Folder("Shop.Web", _root), Is.EqualTo(site));
    }

    /// <summary>louis-agent's own sites are still found where they are.</summary>
    [Test]
    public void ThisRepositorysSites_AreStillFound() =>
        Assert.That(Path.GetFileName(SiteProject.Folder("LouisAgent.Site")), Is.EqualTo("LouisAgent.Site"));
}
