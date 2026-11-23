using LouisAgent.Composition.Sites;

namespace LouisAgent.Core.Tests.Composition.Sites;

/// <summary>
/// F127.7, Louis: one guide for the whole website, selling on the ordinary pages, the demos on /demos, technical on
/// /framework. A page's focus is its longest matching address, else the default. The rule is the shared contract's
/// (F126.3.3), so the widget and the site's API ask the same one.
/// </summary>
public class SiteFocusesTests
{
    /// <summary>louis-agent.dev's parts, as its areas name them.</summary>
    private static SiteFocuses Site() => new("selling",
    [
        new FocusPart("/demos", "demos"), new FocusPart("/guesthouse-demo", "demos"),
        new FocusPart("/framework", "framework"), new FocusPart("/framework/how-it-works/web", "deeper")
    ]);

    /// <summary>A page takes the focus of its longest matching address, its query and its place in it aside.</summary>
    [TestCase("/framework/packs", "framework")]
    [TestCase("/framework", "framework")]
    [TestCase("/framework/how-it-works/web/2", "deeper")]
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
        var focuses = new SiteFocuses(null, [new FocusPart("/demos", "demos")]);

        Assert.That((focuses.Of("/demos/x"), focuses.Of("/pricing")), Is.EqualTo(("demos", (string?)null)));
    }
}
