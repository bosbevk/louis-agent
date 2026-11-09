using System.Reflection;
using LouisAgent.CodeReview.Pages.Site;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Tests.Pages.Site;

/// <summary>
/// What the framework stands on is real (F109.4): every package the Built on it page names is one a project of the
/// framework references; and since the repository is private, no page of the site links to it or calls the source
/// public.
/// </summary>
public class StandsOnTests
{
    /// <summary>Every package reference in the solution's projects, by name.</summary>
    private static HashSet<string> PackagesReferenced()
    {
        var projects = new[] { "src", "packs", "demos" }
            .SelectMany(folder => Directory.GetFiles(Path.Combine(TestPaths.RepoRoot, folder), "*.csproj",
                SearchOption.AllDirectories));

        // A reference reads <PackageReference Include="Name" …/>; the name is between the first pair of quotes after Include=.
        return projects.SelectMany(File.ReadLines)
            .Where(line => line.Contains("<PackageReference Include=\""))
            .Select(line => line.Split("Include=\"")[1].Split('"')[0])
            .ToHashSet();
    }

    /// <summary>Every page of the site, rendered.</summary>
    private static List<string> ThePages()
    {
        using var ctx = SitePageContext.Create();
        var pages = typeof(HomePage).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(typeof(HomePage).Namespace!) == true)
            .Where(type => type.GetCustomAttributes<RouteAttribute>().Any());

        return pages.Select(page => ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        }).Markup).ToList();
    }

    /// <summary>Each package the page names is one the framework really references.</summary>
    [Test]
    public void EveryPackageNamed_IsOneTheFrameworkReferences()
    {
        using var ctx = new BunitContext();
        var referenced = PackagesReferenced();

        var cut = ctx.Render<BuiltOnItPage>();
        var named = cut.FindAll(".stands-on code").Select(code => code.TextContent).ToList();

        Assert.That(referenced, Has.Count.GreaterThan(20), "the project files were read");
        Assert.That(named, Has.Count.GreaterThanOrEqualTo(6), "the page names what it stands on");
        Assert.That(named.Where(package => !referenced.Contains(package)), Is.Empty);
    }

    /// <summary>The repository is private: no page links to it, and none says the source is public.</summary>
    [Test]
    public void NoPage_LinksToThePrivateRepository()
    {
        var pages = ThePages();

        Assert.Multiple(() =>
        {
            Assert.That(pages.Where(page => page.Contains("github.com/bosbevk")), Is.Empty);
            Assert.That(pages.Where(page => page.Contains("are public")), Is.Empty);
        });
    }
}
