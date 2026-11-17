using LouisAgent.Web.Components.Site.Code;

namespace LouisAgent.Web.Tests.Components.Site.Code;

/// <summary>
/// F160.1: a page shows code as a listing named for its file or its language, the code itself as text, so a page that
/// shows a program can't run or break the page with it.
/// </summary>
public class CodeListingTests
{
    /// <summary>The code is shown as text, never read as markup, under its name.</summary>
    [Test]
    public void TheCode_IsText_UnderItsName()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<CodeListing>(listing => listing
            .Add(l => l.Name, "Program.cs")
            .Add(l => l.Language, "csharp")
            .Add(l => l.Code, "var x = new List<string>(); // <b>not bold</b>"));

        var code = cut.Find("figure.code-listing pre code");
        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("figure.code-listing figcaption").TextContent.Trim(), Is.EqualTo("Program.cs"));
            Assert.That(code.TextContent, Is.EqualTo("var x = new List<string>(); // <b>not bold</b>"));
            Assert.That(code.QuerySelectorAll("b"), Is.Empty, "markup in the code stays text");
            Assert.That(code.ClassList, Does.Contain("language-csharp"));
        });
    }

    /// <summary>With no name, the listing is named for its language.</summary>
    [Test]
    public void WithNoName_TheLanguageNamesIt()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<CodeListing>(listing => listing
            .Add(l => l.Language, "yaml")
            .Add(l => l.Code, "name: louis-agent"));

        Assert.That(cut.Find("figcaption").TextContent.Trim(), Is.EqualTo("yaml"));
    }
}
