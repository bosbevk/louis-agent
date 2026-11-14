using LouisAgent.Diagrams.Svg.Theme;
using LouisAgent.Diagrams.Svg.Theme.Looks;

namespace LouisAgent.Diagrams.Tests.Svg.Theme.Looks;

/// <summary>
/// The look every picture in the new style shares (F134.8): one style under the component diagram and the flow, so
/// what they share can't drift apart, and each kind adds only what is its own.
/// </summary>
public class PictureLookTests
{
    private static readonly string Shared = PictureLook.Of(SvgTheme.Site);

    /// <summary>
    /// The shared look carries what any picture in it wears: the ground's dots, the picture's title, a panel by its
    /// depth and its tone, a line with its marching dots, and a port.
    /// </summary>
    [TestCase(".pic-dots")]
    [TestCase(".pic-title")]
    [TestCase(".pic-panel.depth-0")]
    [TestCase(".pic-panel.tone-green")]
    [TestCase(".pic-line")]
    [TestCase(".pic-march")]
    [TestCase(".pic-port")]
    public void TheSharedLook_CarriesWhatEveryPictureWears(string rule)
    {
        Assert.That(Shared, Does.Contain(rule));
    }

    /// <summary>It carries its colours for a light and a dark page, and stops what moves on request.</summary>
    [Test]
    public void TheSharedLook_HasBothThemesAndStopsOnRequest()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Shared, Does.Contain("@media (prefers-color-scheme: dark)"));
            Assert.That(Shared, Does.Contain("@media (prefers-reduced-motion: reduce)"));
            Assert.That(Shared, Does.Contain(".pic-march { display: none; }"));
        });
    }

    /// <summary>
    /// The shared look names nothing that is one kind's own, and a component's own rules name none of the shared
    /// ones again, so each rule is written in one place.
    /// </summary>
    [Test]
    public void EachRule_IsWrittenInOnePlace()
    {
        string components = ComponentsStyle.Of(SvgTheme.Site);

        Assert.Multiple(() =>
        {
            Assert.That(Shared, Does.Not.Contain(".cmp-"), "nothing of a component in the shared look");
            Assert.That(components, Does.Not.Contain(".pic-"), "nothing shared written again for a component");
            Assert.That(components, Does.Contain(".cmp-pin").And.Contain(".cmp-pill").And.Contain(".cmp-ghost"));
        });
    }
}
