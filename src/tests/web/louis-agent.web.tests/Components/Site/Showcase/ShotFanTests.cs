using LouisAgent.Web.Components.Site.Showcase;

namespace LouisAgent.Web.Tests.Components.Site.Showcase;

/// <summary>
/// The fan of photographs shows its reflections only when a site switches them on, as every glass panel does (Louis:
/// "then we can take out reflections from the home page").
/// </summary>
public class ShotFanTests
{
    /// <summary>Three photographs, enough to fan.</summary>
    private static readonly IReadOnlyList<Shot> Three =
    [
        new Shot("A law practice", "It knows what it must not answer.", "/a.webp", "/law-practice-demo"),
        new Shot("A guesthouse", "It opens the room that fits.", "/b.webp", "/guesthouse-demo"),
        new Shot("A restaurant", "It answers at ten at night.", "/c.webp", "/restaurant-demo")
    ];

    /// <summary>With nothing switched, no photograph shows a reflection; switched on, each one does.</summary>
    [Test]
    public void TheShots_ShowReflections_OnlyWhenSwitchedOn()
    {
        using var ctx = new BunitContext();

        var plain = ctx.Render<ShotFan>(p => p.Add(f => f.Items, Three)).FindAll(".shot");
        var shown = ctx.Render<ShotFan>(p => p.Add(f => f.Items, Three).Add(f => f.Reflect, true)).FindAll(".shot");

        Assert.Multiple(() =>
        {
            Assert.That(plain.Any(shot => shot.HasAttribute("data-reflect")), Is.False);
            Assert.That(shown.All(shot => shot.HasAttribute("data-reflect")), Is.True);
        });
    }

    /// <summary>
    /// Each photograph is a piece the visitor can ask about (Louis: "each of these 3 also ask about this"), by an id
    /// from its title.
    /// </summary>
    [Test]
    public void EachShot_IsAPiece()
    {
        using var ctx = new BunitContext();

        var shots = ctx.Render<ShotFan>(p => p.Add(f => f.Items, Three)).FindAll(".shot");

        Assert.Multiple(() =>
        {
            Assert.That(shots.All(shot => shot.HasAttribute("data-piece")), Is.True);
            Assert.That(shots.Select(shot => shot.Id),
                Is.EqualTo(new[] { "a-law-practice", "a-guesthouse", "a-restaurant" }));
        });
    }
}
