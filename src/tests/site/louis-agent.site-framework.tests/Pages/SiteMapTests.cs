using System.Reflection;
using LouisAgent.SiteFramework;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Framework;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.SiteFramework.Tests.Pages;

/// <summary>
/// The framework's site tells the whole framework (ADR 0100, amended in F160.1, Louis: "change the rule then this is
/// framework only stuff"): every project of the framework itself that ships is named on some page, so a new part of
/// the framework can't be left off the site. The demos and the selling site are not the framework.
/// </summary>
public class SiteMapTests
{
    /// <summary>Where the framework's own projects live.</summary>
    private static readonly string[] Framework = ["src/engine/", "src/hosts/", "src/web/", "src/evals/", "src/tools/"];

    /// <summary>The words that end a project only tests use.</summary>
    private static readonly string[] OnlyForTests = [".tests", ".integration-tests", ".testing", ".standin"];

    /// <summary>
    /// The framework's projects that ship: every project in the solution under the framework's folders, but the
    /// tests and the helpers only tests use.
    /// </summary>
    private static List<string> FrameworkProjects() =>
        // A project line reads: Project("{type}") = "name", "path\name.csproj", "{id}": the name is the second quoted
        // part, the path the third.
        File.ReadLines(Path.Combine(TestPaths.RepoRoot, "louis-agent-solution.sln"))
            .Where(line => line.StartsWith("Project(") && line.Contains(".csproj"))
            .Select(line => line.Split('"'))
            .Where(parts => Framework.Any(parts[5].Replace('\\', '/').StartsWith))
            .Select(parts => parts[3])
            .Where(name => !OnlyForTests.Any(name.EndsWith))
            .ToList();

    /// <summary>The whole framework site as a visitor reads it: every page it serves, drawn, in one string.</summary>
    private static string TheWholeSite()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSiteAreas(FrameworkAreas.Name);
        var pages = FrameworkLibrary.Assembly.GetTypes()
            .Where(type => type.GetCustomAttributes<RouteAttribute>().Any());

        // Drawn now, while the context is alive, not lazily after it's gone.
        var markups = pages.Select(page => ctx.Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        }).Markup).ToList();
        return string.Join("\n", markups);
    }

    /// <summary>Every project of the framework that ships has its place on a page.</summary>
    [Test]
    public void EveryFrameworkProject_IsNamedOnAPage()
    {
        var projects = FrameworkProjects();
        string site = TheWholeSite();

        Assert.That(projects, Has.Count.GreaterThanOrEqualTo(30), "the solution file was read");
        Assert.That(projects.Where(project => !site.Contains(project)), Is.Empty);
    }
}
