using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LouisAgent.Site.Tests.Pages;

/// <summary>The code on Build your own is the real code, and the page is honest about the packages.</summary>
public class BuildYourOwnTests
{
    private const string Hosts = "demos/louis-agent/hosts";

    /// <summary>The site, started once for the fixture.</summary>
    private WebApplicationFactory<Program> _site = null!;

    /// <summary>Starts it.</summary>
    [OneTimeSetUp]
    public void StartTheSite() => _site = new WebApplicationFactory<Program>();

    /// <summary>Stops it.</summary>
    [OneTimeTearDown]
    public void StopTheSite() => _site.Dispose();

    /// <summary>The page as a visitor reads it.</summary>
    private async Task<string> PageAsync() =>
        WebUtility.HtmlDecode(await _site.CreateClient().GetStringAsync("/build-your-own"));

    /// <summary>A repo file's lines, without their indentation or the blank ones.</summary>
    private static IEnumerable<string> LinesOf(string file) =>
        File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, file)).Select(line => line.Trim())
            .Where(line => line.Length > 0);

    /// <summary>The program shown is the coding agent's, every line, so the page can't drift from the code.</summary>
    [Test]
    public async Task TheProgramShown_IsTheRealOne()
    {
        string page = await PageAsync();

        var lines = LinesOf($"{Hosts}/louis-agent.coding-cli/Program.cs").ToList();

        Assert.That(lines, Has.Count.GreaterThan(2), "the search found the program");
        Assert.That(lines.Where(line => !page.Contains(line)), Is.Empty);
    }

    /// <summary>The catalogue shown is the real one too: its packs, and how it's made.</summary>
    [Test]
    public async Task TheCatalogueShown_IsTheRealOne()
    {
        string page = await PageAsync();

        var lines = LinesOf($"{Hosts}/louis-agent.coding-catalog/CodingCatalog.cs")
            .Where(line =>
                (line.StartsWith("new ") && line.Contains("Pack(")) || line.Contains("AgentCatalog Create()"))
            .ToList();

        Assert.That(lines, Has.Count.EqualTo(3), "the pack list's two lines, and Create");
        Assert.That(lines.Where(line => !page.Contains(line)), Is.Empty);
    }

    /// <summary>The site shows only what works today, so it says the packages aren't published yet.</summary>
    [Test]
    public async Task ThePage_SaysThePackagesArentOnNuGetYet()
    {
        Assert.That(await PageAsync(), Does.Contain("aren't on NuGet yet"));
    }

    /// <summary>The menu and the home page both lead to it.</summary>
    [Test]
    public async Task TheHomePage_LeadsToIt()
    {
        string home = await _site.CreateClient().GetStringAsync("/");

        Assert.That(home, Does.Contain("<a class=\"btn btn-fill\" href=\"/build-your-own\">"));
        Assert.That(home, Does.Contain("<a href=\"/build-your-own\">"));
    }
}
