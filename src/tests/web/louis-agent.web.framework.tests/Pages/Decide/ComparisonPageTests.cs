using LouisAgent.Web.Docs;
using LouisAgent.Web.Framework.Content.Comparison;
using LouisAgent.Web.Framework.Pages.Decide;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Framework.Tests.Pages.Decide;

/// <summary>
/// F164.1, step 12: Comparison, at <c>/comparison</c>, sets louis-agent beside Microsoft Agent Framework and Semantic
/// Kernel (ADR 0110): each claim about louis-agent names the file that proves it, which the test finds, and the others'
/// cells say only what their own pages say, read on a date the page shows, with each page linked.
/// </summary>
public class ComparisonPageTests
{
    /// <summary>A context the framework's pages draw in: the docs' address their links read.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(DocsAddress.GitHub("bosbevk/louis-agent", "main"));
        return ctx;
    }

    /// <summary>It answers with its heading, the table of the three, and is in the menu.</summary>
    [Test]
    public void ThePage_SetsTheThreeSideBySide()
    {
        using var ctx = Context();

        var cut = ctx.Render<ComparisonPage>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("h1").TextContent.Trim(), Is.EqualTo("Beside the others."));
            Assert.That(cut.FindAll("th[scope=col]").Select(th => th.TextContent.Trim()), Is.SupersetOf(new[]
            {
                "louis-agent", "Microsoft Agent Framework", "Semantic Kernel"
            }));
            Assert.That(FrameworkContents.All.Pages.Select(entry => entry.Address), Does.Contain("/comparison"));
        });
    }

    /// <summary>Every claim about louis-agent names a file of the repo that proves it.</summary>
    [Test]
    public void EveryClaimOfOurs_NamesTheFileThatProvesIt()
    {
        Assert.Multiple(() =>
        {
            foreach (var claim in Comparisons.All)
                Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, claim.Proof)), $"{claim.Topic}: {claim.Proof}");
        });
    }

    /// <summary>The others' pages are linked, with the date they were read on, shown on the page.</summary>
    [Test]
    public void TheOthersPages_AreLinked_WithTheDayTheyWereRead()
    {
        using var ctx = Context();

        var cut = ctx.Render<ComparisonPage>();
        var links = cut.FindAll("section#sources a").Select(link => link.GetAttribute("href")).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("section#sources").TextContent, Does.Contain(Comparisons.ReadOn));
            Assert.That(links, Is.SupersetOf(Comparisons.Sources.Select(source => source.Address)));
        });
    }
}
