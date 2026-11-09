using System.Reflection;
using LouisAgent.CodeReview.Pages.Site;
using LouisAgent.Web.Components.Site.Meta;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.CodeReview.Tests.Pages.Site.Meta;

/// <summary>
/// Every page of the site says what it is about (F121): one description each, in its own words, which a search engine
/// reads from the page the framework serves as HTML. A page that declares one is a page meant for search.
/// </summary>
public class PageDescriptionsTests
{
    /// <summary>Each page of the site declares one description, none empty, no two the same, no semicolon.</summary>
    [Test]
    public void EveryPage_DeclaresItsOwnDescription()
    {
        var pages = typeof(HomePage).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(typeof(HomePage).Namespace!) == true)
            .Where(type => type.GetCustomAttributes<RouteAttribute>().Any())
            .ToList();
        using var ctx = SitePageContext.Create();

        var described = pages.ToDictionary(page => page.Name, page =>
        {
            var cut = ctx.Render(builder =>
            {
                builder.OpenComponent(0, page);
                builder.CloseComponent();
            });
            return cut.FindComponents<PageHead>().Select(found => found.Instance.Description).ToList();
        });

        Assert.Multiple(() =>
        {
            Assert.That(pages, Has.Count.GreaterThanOrEqualTo(10), "the pages were found");
            Assert.That(described.Where(page => page.Value.Count != 1).Select(page => page.Key), Is.Empty,
                "one description a page");
            var texts = described.Values.SelectMany(text => text).ToList();
            Assert.That(texts, Has.All.Length.InRange(50, 170), "long enough to say something, short enough to show");
            Assert.That(texts, Is.Unique);
            Assert.That(texts, Has.None.Contain(";"));
        });
    }
}
