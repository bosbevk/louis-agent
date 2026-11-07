namespace LouisAgent.Site.Tests.Styles;

/// <summary>
/// The site's stylesheet is Sass, on the framework's colours (ADR 0085): every partial is on app.scss's list once,
/// nothing on it is missing, each film's timing is its own so the two never share a keyframe name, and a visitor's
/// reduced-motion setting stills the decoration but never the films (FX35).
/// </summary>
public class SiteStylesheetTests
{
    /// <summary>The site's stylesheet.</summary>
    private static readonly SassStylesheet Css = SassStylesheet.Of("louis-agent.site");

    /// <summary>Every partial is on the list, and everything on the list is there.</summary>
    [Test]
    public void EveryPartial_IsOnTheList_AndEveryNameOnTheListIsThere() =>
        Assert.That(Css.Forwards().Order(), Is.EqualTo(Css.AreaFiles().Order()));

    /// <summary>Its colours are the framework's one copy, listed first, and it keeps none of its own.</summary>
    [Test]
    public void ItsColours_AreTheFrameworks()
    {
        Assert.That(Css.FrameworkForwards(), Is.EqualTo(new[] { "web/settings/colours" }));
        Assert.That(Css.Read(), Does.Contain("--bs-primary: #ffffff"));
        Assert.That(Css.Source("settings/tokens"), Does.Not.Contain("--bs-"));
    }

    /// <summary>Its own values come next, then the plain elements: the rest of the files build on them.</summary>
    [Test]
    public void ItsValuesComeFirst_ThenThePlainElements() =>
        Assert.That(Css.Forwards().Take(2), Is.EqualTo(new[] { "settings/tokens", "base/elements" }));

    /// <summary>Every colour name a rule reads is set, by the framework or by this site, so none falls through.</summary>
    [Test]
    public void EveryNameARuleReads_IsSet()
    {
        string css = Css.Read();
        var read = System.Text.RegularExpressions.Regex.Matches(css, @"var\((--[a-z0-9-]+)").Select(m => m.Groups[1].Value).Distinct();
        var setByAFilm = new[] { "--loop", "--d" };

        Assert.That(read.Except(setByAFilm).Where(name => !css.Contains($"{name}:")), Is.Empty);
    }

    /// <summary>
    /// The two films sit in one stylesheet, so each keyframe name ends in its film's letter (<c>-s</c>, <c>-d</c>) and
    /// no name is declared twice; a shared name would make one film play the other's timing.
    /// </summary>
    [Test]
    public void EachFilm_HasItsOwnKeyframes_AndNoNameIsDeclaredTwice()
    {
        string css = Css.Read();
        var names = css.Split("@keyframes ").Skip(1).Select(part => part[..part.IndexOf(' ')]).ToList();

        Assert.That(names, Is.Unique);
        Assert.That(names.Where(n => n != "bar" && !n.StartsWith("turn-")), Is.All.Matches<string>(BelongsToOneFilm));
    }

    /// <summary>
    /// A PC with its animations switched off asks for reduced motion; that stills the hero's typing but not the films,
    /// which are the content and play as a video would (FX35: with them stilled, a visitor saw every part at once and
    /// no card, caption or light). Every selector in the reduced-motion rule leaves the stage out.
    /// </summary>
    [Test]
    public void ReducedMotion_StillsTheDecoration_AndLeavesTheFilmsPlaying()
    {
        string css = Css.Read();
        string block = css[css.IndexOf("@media (prefers-reduced-motion: reduce)")..];
        block = block[..block.IndexOf("\n}")];
        var selectors = block.Split('{').Skip(1).Select(rule => rule[(rule.LastIndexOf('}') + 1)..].Trim()).Where(s => s.Length > 0)
            .SelectMany(list => list.Split(',')).Select(s => s.Trim()).ToList();

        Assert.That(selectors, Is.Not.Empty, "the reduced-motion rule is there");
        Assert.That(selectors, Is.All.Contains(":not(.stage *)"), "nothing in it reaches a film");
    }

    /// <summary>A film's keyframe name ends in <c>-s</c> or <c>-d</c>, or in that letter and the scene's number.</summary>
    private static bool BelongsToOneFilm(string name)
    {
        string tail = name[(name.LastIndexOf('-') + 1)..];
        return tail is ['s' or 'd', ..] && tail[1..].All(char.IsDigit);
    }
}
