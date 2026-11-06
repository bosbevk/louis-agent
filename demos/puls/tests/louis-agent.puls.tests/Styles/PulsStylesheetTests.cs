using LouisAgent.Testing;

namespace LouisAgent.Puls.Tests.Styles;

/// <summary>
/// Puls's stylesheet is Sass: <c>app.scss</c> is the ordered list of partials, and the build makes one CSS file from
/// it. Every partial is on the list once, nothing on the list is missing, and the colours come first so every later
/// rule can use them. The CSS itself isn't in git, so what's checked here is what the build really made.
/// </summary>
public class PulsStylesheetTests
{
    /// <summary>Puls's stylesheet.</summary>
    private static readonly SassStylesheet Css = SassStylesheet.Of("louis-agent.puls");

    /// <summary>Every partial is on the list, and everything on the list is there.</summary>
    [Test]
    public void EveryPartial_IsOnTheList_AndEveryNameOnTheListIsThere() =>
        Assert.That(Css.Forwards().Order(), Is.EqualTo(Css.AreaFiles().Order()));

    /// <summary>
    /// Every file a rule points at is one the app serves (FX30): the delegation card's Puls tile went missing once when
    /// its rule moved and kept its old path.
    /// </summary>
    [Test]
    public void EveryUrl_PointsAtAFileTheAppServes()
    {
        var urls = Css.Urls();

        Assert.That(urls, Is.Not.Empty, "the delegation card's tile is one");
        Assert.That(urls.Where(u => !File.Exists(u.Target)).Select(u => $"{u.File}: {u.Url}"), Is.Empty);
    }

    /// <summary>The colours are first on the list, then the plain elements: the rest build on them.</summary>
    [Test]
    public void TheColoursComeFirst_ThenThePlainElements() =>
        Assert.That(Css.Forwards().Take(2), Is.EqualTo(new[] { "settings/tokens", "base/elements" }));

    /// <summary>The CSS holds every partial's rules in the list's order, since that decides which rule wins.</summary>
    [Test]
    public void TheCss_HoldsEveryPartial_InTheListsOrder()
    {
        string css = Css.Read();
        int at = 0;

        foreach (string partial in Css.Forwards().Skip(1))
        {
            string selector = FirstSelector(Css.Source(partial));
            int found = css.IndexOf(selector, at, StringComparison.Ordinal);
            Assert.That(found, Is.GreaterThanOrEqualTo(0), $"{partial}'s {selector} comes after the one before");
            at = found;
        }
    }

    /// <summary>A colour is written once, and the build makes both its forms, so the two can't disagree.</summary>
    [Test]
    public void AColour_IsWrittenOnce_AndTheCssHoldsBothItsForms()
    {
        Assert.That(Css.Read(), Does.Contain("--bs-primary: #f58220;").And.Contain("--bs-primary-rgb: 245, 130, 32;"));
        Assert.That(Css.Source("settings/tokens"), Does.Contain("#f58220").And.Not.Contain("245, 130, 32"));
    }

    /// <summary>
    /// A trend's change since the run before (<c>▲ +13%</c>) stays one pill on one line (FX31): in a category card's
    /// small panel it broke into two lines inside its round pill, a green blob beside the value. The small panels fill
    /// their card, two or three across, instead of three fixed columns with one left empty.
    /// </summary>
    [Test]
    public void ATrendsChange_StaysOnOneLine_AndTheSmallPanelsFillTheirCard()
    {
        Assert.That(Rule(".trend-delta {"), Does.Contain("white-space: nowrap"));
        Assert.That(Rule(".trend.compact .trend-grid {"), Does.Contain("repeat(auto-fit, minmax("));
    }

    /// <summary>The first rule in the CSS that starts with this selector, up to its closing brace.</summary>
    private static string Rule(string selector)
    {
        string text = Css.Read();
        int start = text.IndexOf(selector, StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), $"the CSS has {selector}");
        return text[start..(text.IndexOf('}', start) + 1)];
    }

    /// <summary>The start of a partial's first rule, comments skipped: enough to find it again in the CSS.</summary>
    private static string FirstSelector(string source)
    {
        string text = source.TrimStart();
        while (text.StartsWith("/*", StringComparison.Ordinal))
            text = text[(text.IndexOf("*/", StringComparison.Ordinal) + 2)..].TrimStart();

        return text[..text.IndexOfAny([' ', ',', '{', '\n', '\r'])];
    }
}
