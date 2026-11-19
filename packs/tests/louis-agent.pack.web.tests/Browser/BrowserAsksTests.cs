using LouisAgent.Choices;
using LouisAgent.Web.Api;

namespace LouisAgent.Packs.Web.Tests.Browser;

/// <summary>
/// The framework's browser asks for this pack's toolset by name, from a small enum of its own since it can't reference
/// the pack (ADR 0043). This test sits where both are visible, so a rename on either side is caught here: the browser
/// would otherwise ask for a toolset no pack has.
/// </summary>
public class BrowserAsksTests
{
    /// <summary>The web toolset the browser asks for is this pack's, under the same name.</summary>
    [Test]
    public void TheWebToolsetTheBrowserAsksFor_IsThisPacks() =>
        Assert.That(AskedToolset.Web.Wire(), Is.EqualTo(WebToolset.Web.Wire()));
}
