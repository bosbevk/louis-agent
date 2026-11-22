using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Testing.Styles;
using LouisAgent.Web.Themes.Glass;

namespace LouisAgent.Web.Themes.Tests.Glass;

/// <summary>
/// F169.3: the name's ball in glass is the header's share of the letters wherever the name stands (Louis's pick A of
/// three, "the header's share, everywhere"), 0.62, now that the ball takes the name's size. The framework's own
/// default and code review's retro, which draw the same ball, say the same.
/// </summary>
public class GlassWordmarkTests
{
    /// <summary>The header's share of the letters, the one size the name's ball has.</summary>
    private const string HeadersShare = "--web-wordmark-o: .62em;";

    /// <summary>Glass's ball is the header's share, by day and by night.</summary>
    [TestCase(SiteStyle.Dark)]
    [TestCase(SiteStyle.Light)]
    public void GlasssBall_IsTheHeadersShare(SiteStyle style) =>
        Assert.That(ThemeSheet.Values(CompiledThemes.Css(), GlassTheme.Theme, style)
            .GetValueOrDefault("--web-wordmark-o", ""), Is.EqualTo(".62em"));

    /// <summary>The framework's default and retro's two sheets give the ball the same share.</summary>
    [TestCase("src/web/LouisAgent.Web/Styles/web/settings/_surfaces.scss")]
    [TestCase("demos/louis-agent/web/LouisAgent.CodeReview/Styles/retro/_light.scss")]
    [TestCase("demos/louis-agent/web/LouisAgent.CodeReview/Styles/retro/_dark.scss")]
    public void TheDefaultAndRetro_SayTheSame(string sheet) =>
        Assert.That(File.ReadAllText(Path.Combine(TestPaths.RepoRoot, sheet)), Does.Contain(HeadersShare));
}
