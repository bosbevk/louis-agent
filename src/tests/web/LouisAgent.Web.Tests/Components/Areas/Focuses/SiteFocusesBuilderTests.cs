using LouisAgent.Composition.Sites;
using LouisAgent.Web.Components.Areas;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Areas.Focuses;

/// <summary>
/// F127.7: a site names its parts' focuses through the builder among its areas (ADR 0120), and the builder registers
/// the rule it names. How the rule reads an address is the shared contract's, tested there.
/// </summary>
public class SiteFocusesBuilderTests
{
    /// <summary>The builder registers the default and the parts the site names, in its order.</summary>
    [Test]
    public void TheBuilder_RegistersTheRuleTheSiteNames()
    {
        var services = new ServiceCollection().AddSiteAreas(areas => areas.Focuses(focuses => focuses
            .Default("selling")
            .Under("/demos", "demos")
            .Under("/framework", "framework")));

        var focuses = services.BuildServiceProvider().GetRequiredService<SiteFocuses>();

        Assert.That(focuses.Default, Is.EqualTo("selling"));
        Assert.That(focuses.Parts,
            Is.EqualTo(new[] { new FocusPart("/demos", "demos"), new FocusPart("/framework", "framework") }));
    }

    /// <summary>A site that names no focuses has none to look up.</summary>
    [Test]
    public void ASiteWithoutFocuses_RegistersNone() =>
        Assert.That(new ServiceCollection().AddSiteAreas(areas => areas.Chat()).BuildServiceProvider()
            .GetService<SiteFocuses>(), Is.Null);
}
