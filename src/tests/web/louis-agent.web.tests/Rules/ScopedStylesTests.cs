using LouisAgent.Testing;

namespace LouisAgent.Web.Tests.Rules;

/// <summary>
/// A component's own styles sit beside it (CL58): an <c>X.razor.scss</c> belongs to an <c>X.razor</c> in the same
/// folder, holds at least one rule, and a site's list names nothing of a library's but the globals (the account
/// library's and the admin's partials are gone). Every library and site on the framework is read from disk, so a
/// new one comes under the rule by itself.
/// </summary>
public class ScopedStylesTests
{
    private static readonly string[] Libraries =
        ["src/web/louis-agent.web", "src/web/louis-agent.web.accounts", "src/web/louis-agent.web.evals"];

    /// <summary>Every component style file in the libraries and the sites.</summary>
    private static IEnumerable<string> ScopedFiles() =>
        Libraries.Select(library => Path.Combine(TestPaths.RepoRoot, library))
            .Concat(FrameworkWebSites.Folders())
            .SelectMany(folder => Directory.EnumerateFiles(folder, "*.razor.scss", SearchOption.AllDirectories));

    /// <summary>A style file with no component beside it would compile into the bundle and reach nothing.</summary>
    [Test]
    public void AComponentsStyleFile_SitsBesideItsComponent()
    {
        var orphans = ScopedFiles().Where(file => !File.Exists(file[..^5]));

        Assert.That(orphans.Select(Path.GetFileName), Is.Empty);
    }

    /// <summary>An empty style file is one a move left behind.</summary>
    [Test]
    public void AComponentsStyleFile_HoldsARule()
    {
        var empty = ScopedFiles().Where(file => !File.ReadAllText(file).Contains('{'));

        Assert.That(empty.Select(Path.GetFileName), Is.Empty);
    }

    private static IEnumerable<TestCaseData> Sites() =>
        FrameworkWebSites.Folders().Select(folder =>
            new TestCaseData(folder).SetArgDisplayNames(FrameworkWebSites.NameOf(folder)));

    /// <summary>
    /// What a library's components draw alone travels in the bundle with them, so a site's list names only what every
    /// page shares: never an account partial, never an admin one.
    /// </summary>
    [TestCaseSource(nameof(Sites))]
    public void ASitesList_NamesNoLibraryComponentsRules(string folder)
    {
        string list = File.ReadAllText(Path.Combine(folder, "Styles", "app.scss"));

        Assert.That(list, Does.Not.Contain("\"accounts/").And.Not.Contain("\"web/admin/"));
    }
}
