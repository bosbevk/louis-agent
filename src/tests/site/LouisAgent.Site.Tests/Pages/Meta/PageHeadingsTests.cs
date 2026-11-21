using System.Reflection;
using System.Text.RegularExpressions;
using LouisAgent.Api.Site.Rendering;
using LouisAgent.Site.Pages;
using LouisAgent.SiteApi;
using LouisAgent.SiteApi.Resumes;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Tests.Pages.Meta;

/// <summary>
/// FX125: every page the site serves has one h1, its main heading, as the host's prerender gives it to a search
/// engine. The live check on 2026-11-10 found it on two pages of nineteen.
/// </summary>
public partial class PageHeadingsTests
{
    /// <summary>Every page of the site, by its address.</summary>
    private static IEnumerable<TestCaseData> Pages() =>
        typeof(HomePage).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(typeof(HomePage).Namespace!) == true)
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>()
                .Where(route => !route.Template.Contains('{'))
                .Select(route => new TestCaseData(type, route.Template).SetArgDisplayNames(route.Template)));

    /// <summary>The page, as the host serves it, has exactly one h1.</summary>
    [TestCaseSource(nameof(Pages))]
    public async Task EveryPage_HasOneH1(Type page, string address)
    {
        // The host's own list (FX155), with no folder for the CV, so it is the built-in one.
        var prerenderer = new PagePrerenderer(services => SitePageServices.Register(services, new ResumeStore(null)));

        var rendered = await prerenderer.RenderAsync(page, new Uri(new Uri("https://louis-agent.dev"), address));

        Assert.That(rendered, Is.Not.Null, "the page renders");
        Assert.That(H1().Count(rendered!.Body), Is.EqualTo(1));
    }

    /// <summary>
    /// An h1's opening tag: matches <c>&lt;h1 class="x"&gt;</c> and <c>&lt;h1&gt;</c>, not <c>&lt;h10</c>.
    /// </summary>
    [GeneratedRegex(@"<h1[\s>]")]
    private static partial Regex H1();
}
