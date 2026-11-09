using System.Reflection;
using LouisAgent.CodeReview.Pages.Site;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Tests.Pages.Site.Framework;

/// <summary>
/// The site tells the whole framework (F109.4): every project that ships is named on some page of the site, the way
/// the Packs page is held to <c>packs/tools/</c>, so a new part of the solution can't be left off the site.
/// </summary>
public class SiteMapTests
{
    /// <summary>The projects that ship: every project in the solution but the tests and the helpers only tests use.</summary>
    private static List<string> ShippingProjects()
    {
        string[] onlyForTests = [".tests", ".integration-tests", ".testing", ".standin"];

        // A project line reads: Project("{type}") = "name", "path\name.csproj", "{id}". The name is the second quoted part.
        return File.ReadLines(Path.Combine(TestPaths.RepoRoot, "louis-agent-solution.sln"))
            .Where(line => line.StartsWith("Project(") && line.Contains(".csproj"))
            .Select(line => line.Split('"')[3])
            .Where(name => !onlyForTests.Any(name.EndsWith))
            .ToList();
    }

    /// <summary>The whole site as a visitor reads it: every page of the site's namespace, rendered, in one string.</summary>
    private static string TheWholeSite()
    {
        using var ctx = SitePageContext.Create();
        var pages = typeof(HomePage).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(typeof(HomePage).Namespace!) == true)
            .Where(type => type.GetCustomAttributes<RouteAttribute>().Any());

        // Rendered now, while the context is alive, not lazily after it's gone.
        var markups = pages.Select(page => ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        }).Markup).ToList();
        return string.Join("\n", markups);
    }

    /// <summary>Every project that ships has its place on a page.</summary>
    [Test]
    public void EveryProjectThatShips_IsNamedOnAPage()
    {
        var projects = ShippingProjects();
        string site = TheWholeSite();

        Assert.That(projects, Has.Count.GreaterThanOrEqualTo(60), "the solution file was read");
        Assert.That(projects.Where(project => !site.Contains(project)), Is.Empty);
    }
}
