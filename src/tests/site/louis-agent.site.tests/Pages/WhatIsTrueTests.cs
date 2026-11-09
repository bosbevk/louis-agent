using System.Reflection;
using LouisAgent.Site.Pages;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Tests.Pages;

/// <summary>
/// The site says only what is true today (F128.1): what FX122 and F124 changed (one Container App and no VM or
/// databases, Puls not deployed, the guide on three packs) stays out of every page, and the number of design records
/// is held to the folder that keeps them.
/// </summary>
public class WhatIsTrueTests
{
    /// <summary>Every page of the site, rendered, in one string.</summary>
    private static string TheWholeSite()
    {
        using var ctx = SitePageContext.Create();
        var pages = typeof(HomePage).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(typeof(HomePage).Namespace!) == true)
            .Where(type => type.GetCustomAttributes<RouteAttribute>().Any());

        // Rendered now, while the context is alive, not lazily after it's gone.
        return string.Join("\n", pages.Select(page => ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        }).Markup).ToList());
    }

    /// <summary>A claim that stopped being true stays off every page.</summary>
    [TestCase("small VM")]
    [TestCase("two Container Apps")]
    [TestCase("live on mypuls.co.za")]
    [TestCase("one voice on one pack")]
    [TestCase("one voice and one pack")]
    [TestCase("one voice, one pack")]
    [TestCase("The expert in this site")]
    [TestCase("releases both")]
    public void AStaleClaim_IsOnNoPage(string claim) =>
        Assert.That(TheWholeSite(), Does.Not.Contain(claim));

    /// <summary>
    /// The landing's count of design records is the folder's, rounded down to ten: "110+ ADRs" while there are
    /// 110 to 119.
    /// </summary>
    [Test]
    public void TheDesignRecords_AreCounted()
    {
        int records = Directory.GetFiles(Path.Combine(TestPaths.RepoRoot, "src/docs/adr"), "0*.md").Length;
        using var ctx = SitePageContext.Create();

        string landing = ctx.Render<HomePage>().Markup;

        Assert.That(landing, Does.Contain($"{records / 10 * 10}+ ADRs"));
    }
}
