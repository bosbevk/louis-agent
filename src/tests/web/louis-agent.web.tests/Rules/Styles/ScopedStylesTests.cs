using LouisAgent.Testing;

namespace LouisAgent.Web.Tests.Rules.Styles;

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

    /// <summary>
    /// A scoped rule matches only the elements its own component renders (FX63: the message box's rules sat in a
    /// component that draws no element, so Puls's box lost its look). So the class a rule's scope lands on is drawn
    /// by the component itself, or, where the component writes its class from a parameter, passed to it somewhere in
    /// the same library; a rule under <c>::deep</c> alone needs the component to draw an element to hang the scope on.
    /// </summary>
    [Test]
    public void AScopedRule_LandsOnAnElementItsComponentDraws()
    {
        var astray = ScopedFiles().SelectMany(Astray).ToList();

        Assert.That(astray, Is.Empty,
            "move each rule beside the component that draws its element, or under ::deep");
    }

    /// <summary>The selectors in one style file whose scope lands on nothing its component draws.</summary>
    private static IEnumerable<string> Astray(string file)
    {
        string razor = file[..^5];
        string markup = File.ReadAllText(razor);
        string code = File.Exists($"{razor}.cs") ? File.ReadAllText($"{razor}.cs") : "";
        var (classes, dynamic) = ScopedSelectors.Drawn(markup, code);
        string library = LibraryMarkup(file);

        foreach (var (selector, subject) in ScopedSelectors.Subjects(File.ReadAllText(file)))
        {
            bool reached = subject.Length == 0
                ? ScopedSelectors.DrawsAnElement(markup)
                : classes.Contains(subject) || (dynamic && PassedDown(library, subject));
            if (!reached) yield return $"{Path.GetFileName(file)}: {selector}";
        }
    }

    /// <summary>A parent passes the class down as a parameter's value: <c>BoxClass="input-box"</c>.</summary>
    private static bool PassedDown(string library, string subject) =>
        library.Contains($"\"{subject}\"", StringComparison.Ordinal);

    /// <summary>Every component's markup in the project that holds the file.</summary>
    private static string LibraryMarkup(string file)
    {
        string? folder = Path.GetDirectoryName(file);
        while (folder is not null && !Directory.EnumerateFiles(folder, "*.csproj").Any())
            folder = Path.GetDirectoryName(folder);

        return folder is null
            ? ""
            : string.Concat(
                Directory.EnumerateFiles(folder, "*.razor", SearchOption.AllDirectories).Select(File.ReadAllText));
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
