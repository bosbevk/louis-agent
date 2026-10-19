using LouisAgent.Puls.Components.Chat;
using LouisAgent.Puls.Content;

namespace LouisAgent.Puls.Tests.Components;

// T1: a starter's card (F20) on Verken and in Studio, pinned to TemplateCard.html from the first starter; a click hands
// the starter to the page.
public class TemplateCardTests
{
    [Test]
    public void TheFirstStarter_MatchesTheGoldenFile()
    {
        using var ctx = new BunitContext();
        var template = Templates.All[0];

        var cut = ctx.Render<TemplateCard>(p => p.Add(x => x.Template, template));

        Assert.That(cut.Find("strong").TextContent, Is.EqualTo(template.Title));
        Assert.That(cut.Find("small").TextContent, Is.EqualTo($"Deur Puls · {template.Category}"));
        Snapshots.Match("Components/TemplateCard.html", cut.Markup);
    }

    [Test]
    public void AClick_StartsTheTemplate()
    {
        using var ctx = new BunitContext();
        var template = Templates.All[1];
        Template? started = null;

        var cut = ctx.Render<TemplateCard>(p => p.Add(x => x.Template, template).Add(x => x.OnStart, t => started = t));
        cut.Find("button").Click();

        Assert.That(started, Is.SameAs(template));
    }
}
