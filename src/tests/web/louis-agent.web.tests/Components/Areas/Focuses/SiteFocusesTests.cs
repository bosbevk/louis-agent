using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Focuses;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Areas.Focuses;

/// <summary>
/// F127.7, Louis: one guide for the whole website, selling on the ordinary pages, the demos on /demos, technical on
/// /framework. A site names its parts' focuses through the builder (ADR 0120), and a page's focus is its longest
/// matching address, else the default.
/// </summary>
public class SiteFocusesTests
{
    /// <summary>louis-agent.dev's parts, as its areas name them.</summary>
    private static SiteFocuses Site()
    {
        var services = new ServiceCollection().AddSiteAreas(areas => areas.Focuses(focuses => focuses
            .Default("selling")
            .Under("/demos", "demos")
            .Under("/guesthouse-demo", "demos")
            .Under("/framework", "framework")
            .Under("/framework/how-it-works/web", "web")));
        return services.BuildServiceProvider().GetRequiredService<SiteFocuses>();
    }

    /// <summary>A page takes the focus of its longest matching address, its query and its place in it aside.</summary>
    [TestCase("/framework/packs", "framework")]
    [TestCase("/framework", "framework")]
    [TestCase("/framework/how-it-works/web/2", "web")]
    [TestCase("/demos", "demos")]
    [TestCase("/guesthouse-demo?live=1#stay", "demos")]
    [TestCase("http://site.test/framework/identities", "framework")]
    public void APage_TakesItsPartsFocus(string path, string focus) =>
        Assert.That(Site().Of(path), Is.EqualTo(focus));

    /// <summary>Any other page takes the default, and an address matches whole segments only.</summary>
    [TestCase("/pricing")]
    [TestCase("/")]
    [TestCase("/frameworks")]
    public void AnyOtherPage_TakesTheDefault(string path) =>
        Assert.That(Site().Of(path), Is.EqualTo("selling"));

    /// <summary>A site that names no default leaves a page outside its parts without a focus.</summary>
    [Test]
    public void NoDefault_LeavesOtherPagesWithout()
    {
        var services = new ServiceCollection()
            .AddSiteAreas(areas => areas.Focuses(focuses => focuses.Under("/demos", "demos")));
        var focuses = services.BuildServiceProvider().GetRequiredService<SiteFocuses>();

        Assert.That((focuses.Of("/demos/x"), focuses.Of("/pricing")), Is.EqualTo(("demos", (string?)null)));
    }

    /// <summary>A site that names no focuses has none to look up.</summary>
    [Test]
    public void ASiteWithoutFocuses_RegistersNone() =>
        Assert.That(new ServiceCollection().AddSiteAreas(areas => areas.Chat()).BuildServiceProvider()
            .GetService<SiteFocuses>(), Is.Null);
}
