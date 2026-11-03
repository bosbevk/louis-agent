namespace LouisAgent.Site.Tests.Styles;

/// <summary>
/// The site's stylesheet is app.css's import list (ADR 0053): every area file is imported once, nothing imported is
/// missing, the tokens come first, and each film's timing is its own so the two never share a keyframe name.
/// </summary>
public class SiteStylesheetTests
{
    /// <summary>The site's stylesheet.</summary>
    private static readonly Stylesheet Css = Stylesheet.Of("louis-agent.site");

    /// <summary>Every area file is imported, and every import points at a file that's there.</summary>
    [Test]
    public void EveryAreaFile_IsImported_AndEveryImportIsThere() =>
        Assert.That(Css.Imports().Order(), Is.EqualTo(Css.AreaFiles().Order()));

    /// <summary>The tokens are imported first, then the plain elements: the rest of the files build on them.</summary>
    [Test]
    public void TheTokensComeFirst_ThenThePlainElements() =>
        Assert.That(Css.Imports().Take(2), Is.EqualTo(new[] { "settings/tokens.css", "base/elements.css" }));

    /// <summary>The stylesheet reads as the files in their import order, one after the other.</summary>
    [Test]
    public void TheStylesheet_IsTheFilesInImportOrder()
    {
        var starts = Css.Starts();

        Assert.That(starts, Is.All.GreaterThanOrEqualTo(0), "every file is in it");
        Assert.That(starts, Is.Ordered, "in import order");
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

    /// <summary>A film's keyframe name ends in <c>-s</c> or <c>-d</c>, or in that letter and the scene's number.</summary>
    private static bool BelongsToOneFilm(string name)
    {
        string tail = name[(name.LastIndexOf('-') + 1)..];
        return tail is ['s' or 'd', ..] && tail[1..].All(char.IsDigit);
    }
}
