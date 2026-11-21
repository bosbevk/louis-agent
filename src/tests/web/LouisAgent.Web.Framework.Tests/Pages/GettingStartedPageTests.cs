using System.Text.RegularExpressions;
using LouisAgent.Web.Docs;
using LouisAgent.Web.Framework.Content;
using LouisAgent.Web.Framework.Pages;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Framework.Tests.Pages;

/// <summary>
/// F164.1, step 9: Getting started, at <c>/getting-started</c>, shows a program of your own in a few lines, what it
/// loads, whose is what, and where it reads its settings. Its program and its lists are the coding agent's real ones,
/// read from the files, so the page can't drift from the code.
/// </summary>
public partial class GettingStartedPageTests
{
    /// <summary>Where the coding agent's program and its catalogue are.</summary>
    private const string Program = "demos/louis-agent/hosts/LouisAgent.CodingCli/Program.cs",
        Catalogue = "demos/louis-agent/hosts/LouisAgent.Coding.Catalog/CodingCatalog.cs";

    /// <summary>A context the framework's pages draw in: the docs' address their links read.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(DocsAddress.GitHub("bosbevk/louis-agent", "main"));
        // The site's words: the code listing's copy button reads them.
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>A file of the repo, its lines ending as on Linux.</summary>
    private static string Read(string path) =>
        File.ReadAllText(Path.Combine(TestPaths.RepoRoot, path)).ReplaceLineEndings("\n");

    /// <summary>
    /// What a catalogue builds, by its type's name: <c>new FilesPack()</c> and <c>new CodingIdentity()</c> give
    /// <c>FilesPack</c> and <c>CodingIdentity</c>; <c>new AgentCatalog(</c> gives nothing.
    /// </summary>
    [GeneratedRegex(@"new (\w+(?:Pack|Identity))\(")]
    private static partial Regex Built();

    /// <summary>It answers at its address with its heading, then its five sections, each an address of its own.</summary>
    [Test]
    public void ThePage_HasItsHeading_AndItsSections()
    {
        using var ctx = Context();

        var cut = ctx.Render<GettingStartedPage>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("h1").TextContent.Trim(), Is.EqualTo("Your agent, in two lines."));
            Assert.That(cut.FindAll("section[id]").Select(section => section.Id),
                Is.EqualTo(new[] { "top", "what-it-loads", "whose-is-what", "where-it-stands", "where-next" }));
            Assert.That(FrameworkContents.All.Pages.Select(entry => entry.Address), Does.Contain("/getting-started"),
                "in the menu");
        });
    }

    /// <summary>The program it shows is the coding agent's whole <c>Program.cs</c>, as it stands.</summary>
    [Test]
    public void TheProgram_IsTheCodingAgentsWholeFile()
    {
        using var ctx = Context();

        var cut = ctx.Render<GettingStartedPage>();

        Assert.That(cut.Find("section#top figure.code-listing pre code").TextContent + "\n", Is.EqualTo(Read(Program)));
    }

    /// <summary>What it loads names every identity and every pack the coding agent's catalogue builds.</summary>
    [Test]
    public void WhatItLoads_NamesEveryIdentityAndPack()
    {
        using var ctx = Context();
        var built = Built().Matches(Read(Catalogue)).Select(match => match.Groups[1].Value).ToList();

        string loads = ctx.Render<GettingStartedPage>().Find("section#what-it-loads").InnerHtml;

        Assert.Multiple(() =>
        {
            Assert.That(built, Has.Count.GreaterThanOrEqualTo(12), "three identities and nine packs at least");
            Assert.That(built.Where(name => !loads.Contains(name)), Is.Empty);
        });
    }

    /// <summary>
    /// It ends with where to go once the agent answers: the guides that change who it is, give it tools and put it on
    /// a website.
    /// </summary>
    [Test]
    public void ItEnds_WithTheGuidesThatComeNext()
    {
        using var ctx = Context();

        var links = ctx.Render<GettingStartedPage>().FindAll("section#where-next a.doc-link")
            .Select(link => link.GetAttribute("href")).ToList();

        Assert.That(links, Has.Some.EndsWith("build-an-identity.md")
            .And.Some.EndsWith("build-a-pack.md").And.Some.EndsWith("spawn-a-site.md"));
    }

    /// <summary>The run steps are typed where the program's settings are found: its demo's own config folder.</summary>
    [Test]
    public void TheSteps_UseTheDemosOwnSettings()
    {
        using var ctx = Context();

        string steps = ctx.Render<GettingStartedPage>().Find("section#where-it-stands").TextContent;

        Assert.Multiple(() =>
        {
            Assert.That(steps, Does.Contain("demos/louis-agent/config/.env.example"));
            Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, "demos/louis-agent/config/.env.example")));
            Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, "demos/louis-agent/config/.env.secrets.example")));
        });
    }
}
