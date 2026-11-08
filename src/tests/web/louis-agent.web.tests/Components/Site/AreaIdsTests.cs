using LouisAgent.Web.Components.Site.Bands;

namespace LouisAgent.Web.Tests.Components.Site;

/// <summary>
/// A title becomes an id a link and the guide can name (F118): lowercase, words joined by one dash, nothing at either
/// end, and always starting with a letter, which is what the guide's page tool accepts after the #.
/// </summary>
public class AreaIdsTests
{
    /// <summary>The ids the site's card titles get.</summary>
    [TestCase("Puls", "puls")]
    [TestCase("The coding agent", "the-coding-agent")]
    [TestCase("louis-agent.pack.files", "louis-agent-pack-files")]
    [TestCase("Rand for display, with the rate's date", "rand-for-display-with-the-rate-s-date")]
    [TestCase("  The guide!  ", "the-guide")]
    [TestCase("10 fixes", "a-10-fixes")]
    [TestCase("—", "area")]
    public void ATitle_BecomesAnId(string title, string id) =>
        Assert.That(AreaIds.FromWords(title), Is.EqualTo(id));
}
