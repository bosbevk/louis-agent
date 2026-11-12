using System.Text.RegularExpressions;

namespace LouisAgent.Site.Tests.Styles;

/// <summary>
/// The framework's site's stylesheet is Sass: <c>app.scss</c> lists the framework's rules and the triage pack's demo,
/// then the site's look, so it keeps no copy (F124: it left the code-review app with the site). What's checked is
/// what the build really made.
/// </summary>
public class StylesheetTests
{
    /// <summary>The site's stylesheet.</summary>
    private static readonly SassStylesheet Css = SassStylesheet.Of("louis-agent.site");

    /// <summary>The site's three film partials, where every film keyframe lives.</summary>
    private static readonly string[] Films = ["site/movie/stage", "site/movie/short", "site/movie/detail"];

    /// <summary>Every partial of the site's look.</summary>
    private static readonly string[] Site =
    [
        "site/settings/tokens", "site/base/elements", "site/landing/live-turn", "site/landing/sections",
        "site/landing/motion", "site/layout/responsive", "site/chat/guide-form", .. Films
    ];

    /// <summary>Every partial of its own is on the list, and everything on the list is there.</summary>
    [Test]
    public void EveryPartial_IsOnTheList_AndEveryNameOnTheListIsThere() =>
        Assert.That(Css.Forwards().Order(), Is.EqualTo(Css.AreaFiles().Order()));

    /// <summary>It lists the framework's rules rather than keeping a copy, colours first.</summary>
    [Test]
    public void ItListsTheFrameworksRules_ColoursFirst()
    {
        Assert.That(Css.FrameworkForwards()[0], Is.EqualTo("web/settings/colours"));
        Assert.That(Css.FrameworkForwards(), Does.Contain("web/layout/shell").And.Contain("web/base/elements"));
        Assert.That(Css.FrameworkForwards().Where(partial => !File.Exists(Css.FrameworkPartial(partial))), Is.Empty);
    }

    /// <summary>
    /// The site's values come right after the framework's colours, and its plain elements right after the
    /// framework's, so each builds on the one before and the site's win where they meet.
    /// </summary>
    [Test]
    public void TheSitesValuesAndElements_ComeRightAfterTheFrameworks()
    {
        string list = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/site/louis-agent.site/Styles/app.scss"));
        var forwards = Regex.Matches(list, "@forward \"([^\"]+)\"").Select(match => match.Groups[1].Value);

        Assert.That(forwards.Take(4),
            Is.EqualTo(new[]
            {
                "web/settings/colours", "site/settings/tokens", "web/base/elements", "site/base/elements"
            }));
    }

    /// <summary>
    /// The site's look stays inside the site's frame: its element rules hang off the frame's root class, so the chat
    /// and the admin keep the framework's. Only the frame's values sit on the root.
    /// </summary>
    [Test]
    public void TheSitesLook_StaysInsideTheFrame()
    {
        var topLevel = Css.Source("site/base/elements").Split('\n')
            .Where(line => line.Length > 0 && !char.IsWhiteSpace(line[0]) && !line.StartsWith("/*") &&
                           !line.StartsWith('*') && !line.StartsWith("//"))
            .Where(line => !line.StartsWith('}'))
            .ToList();

        Assert.That(topLevel, Is.Not.Empty, "the search found the partial's rules");
        Assert.That(topLevel, Is.All.Matches<string>(line => line.StartsWith(".site")));
        Assert.That(Css.Read(), Does.Contain(".site h1").And.Contain(".site .panel").And.Contain(".site .btn"));
    }

    /// <summary>
    /// The page answers the visitor (FX72, Louis's review against Puls): a panel lifts and glows under the pointer and
    /// a band keeps its room above its content. The index's lit bar is the framework's since F118, tested beside it.
    /// </summary>
    [TestCase(".panel:hover { transform: translateY(-3px);")]
    [TestCase("padding: 64px var(--site-gutter) 40px;")]
    [TestCase(".band-strip { padding-bottom: 72px; }")]
    [TestCase(".btn:hover { transform: translateY(-2px);")]
    [TestCase(".btn-whatsapp, .btn-linkedin { border: 1px solid var(--web-border-hover); color: var(--bs-body-color);")]
    [TestCase(".btn-whatsapp .whatsapp-mark { flex: none; color: var(--whatsapp); }")]
    [TestCase(".btn-whatsapp:hover { border-color: var(--whatsapp);")]
    [TestCase(".btn-linkedin:hover { border-color: var(--linkedin);")]
    [TestCase("to { clip-path: inset(-.2em -.2em -.3em -.2em); translate: 0 0; }")]
    public void TheLook_AnswersTheVisitor(string rule) =>
        Assert.That(
            Css.Source("site/base/elements") + Css.Source("site/landing/motion") + Css.Source("site/landing/sections"),
            Does.Contain(rule));

    /// <summary>
    /// No site partial styles a bare class the chat draws (FX71): the guide's chat sits on every page, and the hero's
    /// turn once drew the chat's turns as its cards. The site prefixes its own names, or hangs them off .site.
    /// </summary>
    [TestCase(".turn")]
    [TestCase(".messages")]
    [TestCase(".user-message")]
    [TestCase(".assistant-message")]
    [TestCase(".composer")]
    public void NoSitePartial_StylesTheChatsClasses(string chatClass)
    {
        var offenders = Site.Where(partial => Css.Source(partial).Split('\n')
                .Any(line => line.StartsWith(chatClass + " ") || line.StartsWith(chatClass + "{") ||
                             line.StartsWith(chatClass + ",") || line.StartsWith(chatClass + ".")))
            .ToList();

        Assert.That(offenders, Is.Empty);
    }

    /// <summary>
    /// F127.4.3, Louis: "our lead form must be styled differently", "not like demos", "not only colors, I do not want
    /// a ticket look", "modern and sleek". The framework's form reads its whole look from <c>--web-form-*</c>, and the
    /// site sets it on its guide's widget only, so the demo businesses' windows keep the plain form.
    /// </summary>
    [Test]
    public void TheGuidesForm_HasTheSitesOwnLook()
    {
        string css = Css.Read();
        string guideForm = Css.Source("site/chat/guide-form");

        Assert.Multiple(() =>
        {
            Assert.That(css, Does.Contain("var(--web-form-radius").And.Contain("var(--web-form-send-bg")
                .And.Contain("var(--web-form-label-transform"));
            Assert.That(guideForm, Does.StartWith("/*").And.Contain(".chat-widget {"));
            Assert.That(guideForm, Does.Contain("--web-form-radius:").And.Contain("--web-form-send-bg: var(--steel)")
                .And.Contain("--web-form-label-transform: uppercase"));
            Assert.That(guideForm, Does.Not.Contain(".site {").And.Not.Contain(".showroom"));
        });
    }

    /// <summary>The triage demo's rules come from the triage pack's web project, so the build found them.</summary>
    [Test]
    public void ThePacksRules_AreInTheBuild() =>
        Assert.That(Css.Read(), Does.Contain(".step-popup").And.Contain(".popup-kind"));

    /// <summary>
    /// Every name the site's rules read is set, by the framework or by the site, so none falls through. The site's
    /// partials only: the framework's rules also read what Bootstrap and the scripts set.
    /// </summary>
    [Test]
    public void EveryNameASiteRuleReads_IsSet()
    {
        string css = Css.Read();
        var read = Site.Select(Css.Source).SelectMany(source => Regex.Matches(source, @"var\((--[a-z0-9-]+)"))
            .Select(m => m.Groups[1].Value).Distinct().ToList();
        var setByAFilm = new[] { "--loop", "--d" };

        Assert.That(read, Does.Contain("--site-gutter"), "the search found the site's names");
        Assert.That(read.Except(setByAFilm).Where(name => !css.Contains($"{name}:")), Is.Empty);
    }

    /// <summary>
    /// The two films sit in one stylesheet, so each keyframe name ends in its film's letter (<c>-s</c>, <c>-d</c>) and
    /// no name is declared twice; a shared name would make one film play the other's timing.
    /// </summary>
    [Test]
    public void EachFilm_HasItsOwnKeyframes_AndNoNameIsDeclaredTwice()
    {
        var names = Films.Select(Css.Source).SelectMany(source => source.Split("@keyframes ").Skip(1))
            .Select(part => part[..part.IndexOf(' ')]).ToList();

        Assert.That(names, Is.Not.Empty, "the search found the films' keyframes");
        Assert.That(names, Is.Unique);
        Assert.That(names.Where(n => n != "bar"), Is.All.Matches<string>(BelongsToOneFilm));
    }

    /// <summary>
    /// A PC with its animations switched off asks for reduced motion; that stills the hero's typing but not the films,
    /// which are the content and play as a video would (FX35). Every selector in the site's reduced-motion rule
    /// leaves the stage out.
    /// </summary>
    [Test]
    public void ReducedMotion_StillsTheDecoration_AndLeavesTheFilmsPlaying()
    {
        string source = Css.Source("site/settings/tokens");
        string block = source[source.IndexOf("@media (prefers-reduced-motion: reduce)")..];
        block = block[..block.IndexOf("\n}")];
        var selectors = block.Split('{').Skip(1).Select(rule => rule[(rule.LastIndexOf('}') + 1)..].Trim())
            .Where(s => s.Length > 0).SelectMany(list => list.Split(',')).Select(s => s.Trim()).ToList();

        Assert.That(selectors, Is.Not.Empty, "the reduced-motion rule is there");
        Assert.That(selectors, Is.All.Contains(":not(.stage *)"), "nothing in it reaches a film");
    }

    /// <summary>
    /// A film's keyframe name ends in <c>-s</c> or <c>-d</c>, or in that letter and the scene's number.
    /// </summary>
    private static bool BelongsToOneFilm(string name)
    {
        string tail = name[(name.LastIndexOf('-') + 1)..];
        return tail is ['s' or 'd', ..] && tail[1..].All(char.IsDigit);
    }
}
