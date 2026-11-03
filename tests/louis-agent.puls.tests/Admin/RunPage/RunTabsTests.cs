using LouisAgent.Choices;
using LouisAgent.Puls.Admin.RunPage;

namespace LouisAgent.Puls.Tests.Admin.RunPage;

/// <summary>
/// The run page's tabs (F53.1 step 2): the one in the address, read once; the summary when the address names none
/// or one that doesn't exist; and the tabs in the order the bar shows them, each with its wire name for the address.
/// </summary>
public class RunTabsTests
{
    /// <summary>The address names a tab by its wire name, in any case.</summary>
    [TestCase("notes", RunTab.Notes)]
    [TestCase("Conversations", RunTab.Conversations)]
    [TestCase("judge", RunTab.Judge)]
    [TestCase("settings", RunTab.Settings)]
    public void TheAddressNamesATab_ByItsWireName(string query, RunTab tab) =>
        Assert.That(RunTabs.Parse(query), Is.EqualTo(tab));

    /// <summary>No tab in the address, or one that doesn't exist, opens the summary.</summary>
    [TestCase(null)]
    [TestCase("")]
    [TestCase("recipes")]
    public void NoTabOrAnUnknownOne_OpensTheSummary(string? query) =>
        Assert.That(RunTabs.Parse(query), Is.EqualTo(RunTab.Summary));

    /// <summary>The bar shows the tabs in reading order: the answer first, the explorer, then the rest.</summary>
    [Test]
    public void TheBar_ShowsTheTabsInReadingOrder() =>
        Assert.That(RunTabs.All.Select(t => t.Tab.Wire()),
            Is.EqualTo(new[] { "summary", "conversations", "judge", "notes", "settings" }));
}
