using System.Net;
using LouisAgent.CodeReview.Pages.Site;
using LouisAgent.CodeReview.Pages.Site.Framework;

namespace LouisAgent.CodeReview.Tests.Pages.Site;

/// <summary>The code on Build your own is the real code, and the page is honest about the packages.</summary>
public class BuildYourOwnTests
{
    private const string Hosts = "demos/louis-agent/hosts";

    /// <summary>The page as a visitor reads it.</summary>
    private static string Page()
    {
        using var ctx = new BunitContext();
        return WebUtility.HtmlDecode(ctx.Render<BuildYourOwnPage>().Markup);
    }

    /// <summary>A repo file's lines, without their indentation or the blank ones.</summary>
    private static IEnumerable<string> LinesOf(string file) =>
        File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, file)).Select(line => line.Trim())
            .Where(line => line.Length > 0);

    /// <summary>The program shown is the coding agent's, every line, so the page can't drift from the code.</summary>
    [Test]
    public void TheProgramShown_IsTheRealOne()
    {
        string page = Page();
        var lines = LinesOf($"{Hosts}/louis-agent.coding-cli/Program.cs").ToList();

        Assert.That(lines, Has.Count.GreaterThan(2), "the search found the program");
        Assert.That(lines.Where(line => !page.Contains(line)), Is.Empty);
    }

    /// <summary>The catalogue shown is the real one too: its packs, and how it's made.</summary>
    [Test]
    public void TheCatalogueShown_IsTheRealOne()
    {
        string page = Page();
        var lines = LinesOf($"{Hosts}/louis-agent.coding-catalog/CodingCatalog.cs")
            .Where(line =>
                (line.StartsWith("new ") && line.Contains("Pack(")) || line.Contains("AgentCatalog Create()"))
            .ToList();

        Assert.That(lines, Has.Count.EqualTo(3), "the pack list's two lines, and Create");
        Assert.That(lines.Where(line => !page.Contains(line)), Is.Empty);
    }

    /// <summary>The site shows only what works today, so it says the packages aren't published yet.</summary>
    [Test]
    public void ThePage_SaysThePackagesArentOnNuGetYet()
    {
        Assert.That(Page(), Does.Contain("aren't on NuGet yet"));
    }
}
