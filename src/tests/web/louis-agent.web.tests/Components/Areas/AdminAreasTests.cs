using LouisAgent.Testing.Messages;
using LouisAgent.Web.Components.Admin;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Messages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Areas;

/// <summary>
/// F144, Louis: "admin must be the main admin layout for admin areas to plugin into". The admin is a frame, and each
/// area inside it brings a card for the admin's home, as it brings its sidebar links; one named without the frame is
/// refused with the reason (his pick: a web message enum).
/// </summary>
public class AdminAreasTests
{
    private sealed class Layout : LayoutComponentBase;

    private sealed class FirstCard : ComponentBase;

    private sealed class SecondCard : ComponentBase;

    /// <summary>An area with a card and nothing else.</summary>
    private static SiteArea CardArea(string name, Type card) => new(name, _ => { }, [], AdminCard: card);

    /// <summary>The areas the site names.</summary>
    private static SiteAreas Named(Action<SiteAreasBuilder> name) =>
        new ServiceCollection().AddSiteAreas(name).BuildServiceProvider().GetRequiredService<SiteAreas>();

    /// <summary>The home's cards come in the order the site named their areas.</summary>
    [Test]
    public void TheCards_ComeInTheOrderNamed() =>
        Assert.That(Named(areas => areas.Admin(typeof(Layout))
                .Add(CardArea("second", typeof(SecondCard))).Add(CardArea("first", typeof(FirstCard)))).AdminCards,
            Is.EqualTo(new[] { typeof(SecondCard), typeof(FirstCard) }));

    /// <summary>An admin area named without the frame is refused at start-up, naming the area and the cure.</summary>
    [Test]
    public void AnAdminArea_WithoutTheFrame_IsRefused()
    {
        var refused = Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddSiteAreas(areas => areas.Add(CardArea("overview", typeof(FirstCard)))));

        Assert.That(refused!.Message, Does.Contain("overview").And.Contain(".Admin(layout)"));
    }

    /// <summary>The card's frame draws its title, what the area put in it, and the way to the area.</summary>
    [Test]
    public void TheCardsFrame_DrawsItsTitleBodyAndWayIn()
    {
        using var ctx = new BunitContext();

        var card = ctx.Render<AdminCard>(parameters => parameters.Add(c => c.Title, "Overview")
            .Add(c => c.Open, "admin/overview").AddChildContent("<p class=\"body\">12 talks</p>"));

        Assert.Multiple(() =>
        {
            Assert.That(card.Find(".admin-card-title").TextContent, Is.EqualTo("Overview"));
            Assert.That(card.Find(".body").TextContent, Is.EqualTo("12 talks"));
            Assert.That(card.Find("a.admin-card-open").GetAttribute("href"), Is.EqualTo("admin/overview"));
        });
    }

    /// <summary>ADR 0119: the web's messages each match their holes record.</summary>
    [Test]
    public void EveryHolesRecord_MatchesItsTemplate() =>
        Assert.That(MessageHoles.Problems([typeof(WebErrorMessage)]), Is.Empty);
}
