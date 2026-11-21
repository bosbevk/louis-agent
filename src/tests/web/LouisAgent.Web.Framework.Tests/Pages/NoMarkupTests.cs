using System.Text.RegularExpressions;

namespace LouisAgent.Web.Framework.Tests.Pages;

/// <summary>
/// F164.1: the framework's pages hold no HTML (ADR 0125): each is configured framework parts given their words and
/// data, so a page can't grow a look of its own the theme doesn't reach. A tag in lower case is HTML; a component is
/// named in capitals.
/// </summary>
public partial class NoMarkupTests
{
    /// <summary>
    /// An HTML tag opening: a <c>&lt;</c> and a lower-case letter. Matches <c>&lt;p class=</c>; not <c>&lt;Band</c>.
    /// </summary>
    [GeneratedRegex("<[a-z][a-z0-9]*[\\s>/]", RegexOptions.NonBacktracking)]
    private static partial Regex HtmlTag();

    /// <summary>No page of the framework's holds a tag of HTML.</summary>
    [Test]
    public void NoFrameworkPage_HoldsHtml()
    {
        string pages = Path.Combine(TestPaths.RepoRoot, "src/web/LouisAgent.Web.Framework/Pages");

        var withHtml = Directory.EnumerateFiles(pages, "*.razor", SearchOption.AllDirectories)
            .Where(page => HtmlTag().IsMatch(File.ReadAllText(page)))
            .Select(Path.GetFileName);

        Assert.That(withHtml, Is.Empty);
    }
}
