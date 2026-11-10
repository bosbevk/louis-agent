using LouisAgent.Web.Components.Site.Showroom.Mock;

namespace LouisAgent.Web.Tests.Components.Site.Showroom.Mock;

/// <summary>
/// F127.2 step 9 (Louis: "rework the branding to be world class and fancy"): a mock website wears a brand of its own,
/// its faces as a class, its header, and a fourth colour for its small capitals. Its faces' rules and files are held
/// where they're built, by the site that wears them (<c>ShowroomFacesTests</c>).
/// </summary>
public class MockBrandsTests
{
    /// <summary>
    /// A mock wears its header (mockup 0: a band of its brand colour, a page-coloured header under a rule, or one over
    /// a double line), and its labels' colour, the on-brand colour unless it has its own.
    /// </summary>
    [Test]
    public void AMock_WearsItsHeader_AndItsLabelColour()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        var plain = ctx.Render<SiteMock>(parameters => parameters
            .Add(mock => mock.Colours, new MockColours("#14213d", "#f6f2ea", "#f6f2ea", "#1b2233"))
            .Add(mock => mock.Agent, "<div></div>"));
        var ruled = ctx.Render<SiteMock>(parameters => parameters
            .Add(mock => mock.Colours, new MockColours("#14213d", "#f6f2ea", "#f6f2ea", "#1b2233", "#b08d57"))
            .Add(mock => mock.Header, MockHeader.Ruled)
            .Add(mock => mock.Font, MockFont.Bodoni)
            .Add(mock => mock.Agent, "<div></div>"));

        Assert.Multiple(() =>
        {
            Assert.That(plain.Find(".site-mock").ClassList, Does.Contain("head-band"));
            Assert.That(plain.Find(".site-mock").GetAttribute("style"), Does.Contain("--mock-label: #f6f2ea"));
            Assert.That(ruled.Find(".site-mock").ClassList, Does.Contain("head-ruled").And.Contain("font-bodoni"));
            Assert.That(ruled.Find(".site-mock").GetAttribute("style"), Does.Contain("--mock-label: #b08d57"));
        });
    }

    /// <summary>A label colour that isn't one is refused, as the other colours are.</summary>
    [Test]
    public void ALabelColourThatIsntOne_IsRefused() =>
        Assert.Throws<ArgumentException>(() =>
            _ = new MockColours("#14213d", "#f6f2ea", "#f6f2ea", "#1b2233", "brass; color: red"));
}
