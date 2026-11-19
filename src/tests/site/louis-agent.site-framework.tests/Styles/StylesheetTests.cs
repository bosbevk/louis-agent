using LouisAgent.Web.Testing.Styles;

namespace LouisAgent.SiteFramework.Tests.Styles;

/// <summary>
/// The framework's site's stylesheet is Sass: <c>app.scss</c> lists the framework's rules and the flat theme, and
/// keeps no copy (F164.1). What's checked is what the build really made.
/// </summary>
public class StylesheetTests
{
    /// <summary>The site's stylesheet.</summary>
    private static readonly SassStylesheet Css = SassStylesheet.Of("louis-agent.site-framework");

    /// <summary>
    /// The framework Orbi's chat (F164.2) is drawn as the chat is on every site: its box and its send button come
    /// from the framework's chat rules, so the site lists them.
    /// </summary>
    [TestCase(".input-row textarea {")]
    [TestCase(".composer .send, .composer .stop {")]
    public void TheChat_HasItsStyles(string rule) =>
        Assert.That(Css.Read(), Does.Contain(rule));

    /// <summary>Every name on the list is one of the framework's partials, and it is there.</summary>
    [Test]
    public void ItListsTheFrameworksRules_AndEachIsThere() =>
        Assert.That(Css.FrameworkForwards().Where(partial => !File.Exists(Css.FrameworkPartial(partial))), Is.Empty);
}
