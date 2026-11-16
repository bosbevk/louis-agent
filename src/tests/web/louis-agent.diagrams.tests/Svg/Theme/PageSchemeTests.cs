using System.Text.RegularExpressions;
using LouisAgent.Diagrams.Engine.Svg.Theme;
using LouisAgent.Diagrams.Engine.Svg.Theme.Looks;

namespace LouisAgent.Diagrams.Tests.Svg.Theme;

/// <summary>
/// F151.7: a picture drawn into one of our pages turns with the page's Dark and Light switch, and a picture read as a
/// file still follows the device. Each style a picture writes its colours in has, after the device's dark rule, a
/// rule for a page that says dark and one for a page that says light, and they set the same names the device's rule
/// does.
/// </summary>
public partial class PageSchemeTests
{
    /// <summary>The device's rule for a dark screen, as every style opens it.</summary>
    private const string Device = "@media (prefers-color-scheme: dark) {";

    /// <summary>The rule for a page whose <c>data-style</c> says dark.</summary>
    private const string DarkPage = "[data-style=\"dark\"] svg {";

    /// <summary>The rule for a page whose <c>data-style</c> says light.</summary>
    private const string LightPage = "[data-style=\"light\"] svg {";

    /// <summary>
    /// A variable a block sets. It finds <c>violet-fill</c> in "--violet-fill: #110a40;", and nothing in
    /// "var(--violet-fill)", which reads it.
    /// </summary>
    [GeneratedRegex(@"--([a-z0-9-]+):", RegexOptions.NonBacktracking)]
    private static partial Regex Set();

    /// <summary>Every style that sets a picture's colours: the theme's own, and the two looks'.</summary>
    private static IEnumerable<TestCaseData> Styles()
    {
        yield return new TestCaseData(SvgStyle.Of(SvgTheme.Site)).SetName("TheThemesColours");
        yield return new TestCaseData(PictureLook.Of(SvgTheme.Site)).SetName("ThePictureLook");
        yield return new TestCaseData(MarqueeLook.Rules).SetName("TheMarqueeLook");
    }

    /// <summary>The variables set between a block's opening words and the brace that closes it.</summary>
    private static HashSet<string> SetIn(string style, string opening)
    {
        int at = style.IndexOf(opening, StringComparison.Ordinal);
        Assert.That(at, Is.GreaterThanOrEqualTo(0), $"the style has {opening}");
        string block = style[(at + opening.Length)..];
        return [.. Set().Matches(block[..block.IndexOf('}')]).Select(match => match.Groups[1].Value)];
    }

    /// <summary>A page that says dark, and one that says light, set every colour the device's dark rule sets.</summary>
    [TestCaseSource(nameof(Styles))]
    public void APageThatSaysWhichItWears_SetsEveryColour(string style)
    {
        var device = SetIn(style, Device);

        Assert.Multiple(() =>
        {
            Assert.That(device, Is.Not.Empty);
            Assert.That(SetIn(style, DarkPage), Is.EquivalentTo(device), "a dark page");
            Assert.That(SetIn(style, LightPage), Is.EquivalentTo(device), "a light page");
        });
    }

    /// <summary>The page's rules come after the device's, so where both apply the page decides.</summary>
    [TestCaseSource(nameof(Styles))]
    public void ThePagesRules_ComeAfterTheDevices(string style)
    {
        int device = style.IndexOf(Device, StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(style.IndexOf(DarkPage, StringComparison.Ordinal), Is.GreaterThan(device));
            Assert.That(style.IndexOf(LightPage, StringComparison.Ordinal), Is.GreaterThan(device));
        });
    }

    /// <summary>The page's words are the engine's own, named once, so a page and a picture agree on them.</summary>
    [Test]
    public void ThePagesWords_AreNamedOnce() => Assert.Multiple(() =>
    {
        Assert.That(PageScheme.Attribute, Is.EqualTo("data-style"));
        Assert.That(PageScheme.Dark, Is.EqualTo("dark"));
        Assert.That(PageScheme.Light, Is.EqualTo("light"));
    });
}
