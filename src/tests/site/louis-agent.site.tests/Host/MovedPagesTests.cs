using System.Reflection;
using LouisAgent.SiteApi;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Tests.Host;

/// <summary>
/// The framework's pages moved under <c>/framework</c> (F128.1, Louis: "the page names must change now"), so the
/// site can sell first and the framework's pages can later move whole to their own site. An old address, from a
/// bookmark or a search result, still lands where its place is now.
/// </summary>
public class MovedPagesTests
{
    /// <summary>An old address leads to its new one, with its section and its page of a section kept.</summary>
    [TestCase("/doctors-practice-demo", "/guesthouse-demo")]
    [TestCase("/how-it-works", "/framework/how-it-works")]
    [TestCase("/how-it-works/loop/2", "/framework/how-it-works/loop/2")]
    [TestCase("/architecture", "/framework/architecture")]
    [TestCase("/identities/levels", "/framework/identities/levels")]
    [TestCase("/packs", "/framework/packs")]
    [TestCase("/build-your-own", "/framework/build-your-own")]
    [TestCase("/proof", "/framework/measured")]
    [TestCase("/proof/rand", "/framework/measured/rand")]
    [TestCase("/built-on-it", "/framework/built-on-it")]
    [TestCase("/whats-coming", "/framework/whats-coming")]
    [TestCase("/Packs/", "/framework/packs")]
    public void AnOldAddress_LeadsToItsNewOne(string old, string now) =>
        Assert.That(MovedPages.Where(old), Is.EqualTo(now));

    /// <summary>An address that never moved, or only starts like one that did, stays where it is.</summary>
    [TestCase("/")]
    [TestCase("/contact")]
    [TestCase("/demos")]
    [TestCase("/proofs")]
    [TestCase("/framework/packs")]
    public void AnAddressThatNeverMoved_StaysWhereItIs(string address) =>
        Assert.That(MovedPages.Where(address), Is.Null);

    /// <summary>Every new address is a page of the site, so no old link leads nowhere.</summary>
    [Test]
    public void EveryNewAddress_IsAPage()
    {
        var pages = typeof(App).Assembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>()).Select(route => route.Template).ToList();

        Assert.That(MovedPages.Moves.Values, Is.SubsetOf(pages));
    }

    /// <summary>The site's host sends the old addresses on before the pages answer.</summary>
    [Test]
    public void TheProgram_MapsTheMovedPages()
    {
        string program = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/site/louis-agent.site-api/Program.cs"));

        Assert.That(program.IndexOf("app.MapMovedPages()", StringComparison.Ordinal),
            Is.GreaterThan(0).And.LessThan(program.IndexOf("app.MapLouisAgentSite(", StringComparison.Ordinal)));
    }
}
