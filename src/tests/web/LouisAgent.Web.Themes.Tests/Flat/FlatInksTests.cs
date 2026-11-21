using LouisAgent.Choices;
using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Testing.Styles;
using LouisAgent.Web.Themes.Flat;
using LouisAgent.Web.Themes.Glass;

namespace LouisAgent.Web.Themes.Tests.Flat;

/// <summary>
/// F169.1: every ink flat writes in reads on every ground it stands on, in both styles, at the 4.5 to 1 the web's
/// guidelines ask of words (Louis's pick B of the design review's finding 11: "A, and a test holds it"). The pairs
/// are the ones measured on the running site: the words, the quiet words and the faint words on the page, on a pane
/// and in the search window, and the words and the quiet words on the menu's grey. A comment in a code listing stands
/// on the code grey (Louis's pick: code comments take the firmer faint grey too).
/// </summary>
public class FlatInksTests
{
    /// <summary>The words, the quiet words and the faint words.</summary>
    private static readonly string[] AllInks = ["--bs-body-color", "--bs-secondary-color", "--bs-tertiary-color"];

    /// <summary>The words and the quiet words: the faint words never stand on the menu's grey.</summary>
    private static readonly string[] MenuInks = ["--bs-body-color", "--bs-secondary-color"];

    /// <summary>The page, a pane and the search window, where every ink stands.</summary>
    private static readonly string[] Grounds = ["--bs-body-bg", "--web-pane-fill", "--web-dialog-bg"];

    /// <summary>The menu's grey.</summary>
    private const string MenuGround = "--bs-tertiary-bg";

    /// <summary>A comment in a code listing.</summary>
    private const string CommentInk = "--web-code-comment";

    /// <summary>The code grey a listing stands on.</summary>
    private const string CodeGround = "--web-code-bg";

    /// <summary>Every ink on every ground it stands on, in each style.</summary>
    private static IEnumerable<TestCaseData> Pairs() =>
        from style in new[] { SiteStyle.Light, SiteStyle.Dark }
        from pair in AllInks.SelectMany(ink => Grounds.Select(ground => (ink, ground)))
            .Concat(MenuInks.Select(ink => (ink, ground: MenuGround)))
            .Append((ink: CommentInk, ground: CodeGround))
        select new TestCaseData(style, pair.ink, pair.ground);

    /// <summary>The page's ground.</summary>
    private const string Page = "--bs-body-bg";

    /// <summary>The purple a page's topic line is written in.</summary>
    private const string Purple = "--web-purple";

    /// <summary>
    /// The looks whose page the purple is held to: flat in both styles, and glass in the dark. Glass's light page is
    /// a pale violet the purple reads 2.9 to 1 on, as the brand's violet did before; that is glass's own look.
    /// </summary>
    private static IEnumerable<TestCaseData> Looks() =>
        from look in new[]
        {
            (Theme: FlatTheme.Theme, Style: SiteStyle.Light),
            (Theme: FlatTheme.Theme, Style: SiteStyle.Dark),
            (Theme: GlassTheme.Theme, Style: SiteStyle.Dark),
        }
        select new TestCaseData(look.Theme, look.Style).SetArgDisplayNames(look.Theme.Id, look.Style.Wire());

    /// <summary>
    /// A page's topic line reads on the page: the purple each theme lightens for the dark (Louis's pick B of finding
    /// 11 brought it: dark's topic line was the brand's violet, 3.84 to 1).
    /// </summary>
    [TestCaseSource(nameof(Looks))]
    public void ThePurple_ReadsOnThePage(SiteTheme theme, SiteStyle style)
    {
        var values = ThemeSheet.Values(CompiledThemes.Css(), theme, style);

        Assert.That(Contrast.Between(values[Purple], values[Page]), Is.GreaterThanOrEqualTo(Contrast.ForWords),
            $"{Purple} {values[Purple]} on {values[Page]}");
    }

    /// <summary>An ink reads on a ground it stands on.</summary>
    [TestCaseSource(nameof(Pairs))]
    public void AnInk_ReadsOnItsGround(SiteStyle style, string ink, string ground)
    {
        var values = ThemeSheet.Values(CompiledThemes.Css(), FlatTheme.Theme, style);

        Assert.That(Contrast.Between(values[ink], values[ground]), Is.GreaterThanOrEqualTo(Contrast.ForWords),
            $"{ink} {values[ink]} on {ground} {values[ground]}");
    }
}
