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
        new Shot("A law practice", "It knows what it must not answer.", "/a.webp", "/law-practice-demo", "koringland.example"),
        new Shot("A guesthouse", "It opens the room that fits.", "/b.webp", "/guesthouse-demo", "seemeeu.example"),
        new Shot("A restaurant", "It answers at ten at night.", "/c.webp", "/restaurant-demo", "wheatfieldkitchen.example")
    ];

    /// <summary>
    /// Each photograph stands in a glass window, its bar with the three dots and the business's address, and the window
    /// holds the photograph and its caption alone, no chat (Louis: "just glass frames with the 3 dots, no chats").
    /// </summary>
    [Test]
    public void EachShot_IsAGlassWindow_WithItsAddress_AndNoChat()
    {
        using var ctx = new BunitContext();

        var fan = ctx.Render<ShotFan>(p => p.Add(f => f.Items, Three));
        var windows = fan.FindAll(".shot > .framed-window");

        Assert.Multiple(() =>
        {
            Assert.That(windows, Has.Count.EqualTo(3));
            Assert.That(windows.Select(window => window.QuerySelectorAll(".window-dots i").Length),
                Is.All.EqualTo(3));
            Assert.That(windows.Select(window => window.QuerySelector(".window-address")!.TextContent),
                Is.EqualTo(new[] { "koringland.example", "seemeeu.example", "wheatfieldkitchen.example" }));
            Assert.That(windows.Select(window => window.QuerySelector(".window-body img")?.GetAttribute("src")),
                Is.EqualTo(new[] { "/a.webp", "/b.webp", "/c.webp" }));
            Assert.That(fan.FindAll(".body-chat"), Is.Empty);
        });
    }

    /// <summary>
    /// A window that comes out casts its shadow at an angle, down and to the right from a light at the top left, and
    /// glows in no colour (Louis: "come out with an angled shadow not a glow").
    /// </summary>
    [Test]
    public void AWindowThatComesOut_CastsAnAngledShadow_NotAGlow()
    {
        string films = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Styles/web/tools/surface/_films.scss"));
        string lifted = films[films.IndexOf("@mixin shot-lifted()", StringComparison.Ordinal)..];
        lifted = lifted[..(lifted.IndexOf('}') + 1)];

        Assert.Multiple(() =>
        {
            Assert.That(lifted, Does.Not.Contain("--bs-primary"));
            Assert.That(lifted, Does.Not.Contain("box-shadow: 0 "));
            Assert.That(lifted, Does.Contain("--web-shadow-rgb"));
        });
    }

    /// <summary>
    /// The window the visitor picked, or the one the agent shows, comes to the front, and the middle one steps back
    /// for it (Louis: "when one is selected it must go to the front").
    /// </summary>
    [Test]
    public void APickedShot_ComesToTheFront()
    {
        string styles = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "src/web/louis-agent.web/Components/Site/Showcase/ShotFan.razor.scss"));

        Assert.Multiple(() =>
        {
            Assert.That(styles, Does.Contain(".shot:is([data-selected], [data-lit])"));
            Assert.That(styles, Does.Contain(".shot-fan:has(> :is([data-selected], [data-lit]))"));
        });
    }

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
