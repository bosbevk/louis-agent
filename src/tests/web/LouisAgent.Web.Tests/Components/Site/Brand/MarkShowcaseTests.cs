using LouisAgent.Web.Components.Site.Brand;

namespace LouisAgent.Web.Tests.Components.Site.Brand;

/// <summary>
/// FX169: a site shows its mark large, the agent's orb with its letter, and, when it asks, its name with the mark as
/// its letter on four grounds, black, white, violet and glass (Louis: "a slide dedicated to branding").
/// </summary>
public class MarkShowcaseTests
{
    /// <summary>The orb large, with its letter, and the name on the four grounds when asked for.</summary>
    [Test]
    public void TheMark_IsTheOrbLarge_AndTheNameOnFourGrounds()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<MarkShowcase>(p => p
            .Add(m => m.Name, "louis-agent").Add(m => m.Letter, "l")
            .Add(m => m.Before, "l").Add(m => m.After, "uis-agent").Add(m => m.Lockups, true));

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".mark-showcase .mark-large .orb").TextContent.Trim(), Is.EqualTo("l"));
            Assert.That(cut.FindAll(".mark-grounds > .mark-ground").Select(ground => ground.ClassList[1]),
                Is.EqualTo(new[] { "on-black", "on-white", "on-violet", "on-glass" }));
            Assert.That(cut.FindAll(".mark-grounds .wordmark"), Has.Count.EqualTo(4));
        });
    }

    /// <summary>Without lockups asked for, it is the orb alone.</summary>
    [Test]
    public void WithoutLockups_ItIsTheOrbAlone()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<MarkShowcase>(p => p.Add(m => m.Name, "louis-agent").Add(m => m.Letter, "l"));

        Assert.That(cut.FindAll(".mark-grounds"), Is.Empty);
    }
}
