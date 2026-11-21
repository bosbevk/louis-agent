using LouisAgent.Testing;
using LouisAgent.Web.Components.Site.Pieces;
using LouisAgent.Web.Components.Site.Product;
using LouisAgent.Web.Components.Site.Product.Page;
using LouisAgent.Web.Components.Site.Product.Scene;

namespace LouisAgent.Web.Tests.Components.Site.Product;

/// <summary>
/// The design's product window (FX163, Louis: "this could be the example", "then put each of these inside"): a
/// business's own page with the agent's chat beside it, one scene for each business, which the site gives as records
/// and the window plays in turn, in a glass frame like every panel.
/// </summary>
public class ProductWindowTests
{
    /// <summary>The guesthouse: three rooms, the garden room the agent picks, a question and its answer.</summary>
    private static readonly ProductScene Guesthouse = new(
        "seemeeu.example", "Seemeeu Guesthouse", ["HOME", "ROOMS", "STAY"],
        [
            new ProductRoom("/sea.webp", "The sea-view doubles", "R1,450 a night"),
            new ProductRoom("/family.webp", "The family room", "R1,850 a night"),
            new ProductRoom("/garden.webp", "The garden room", "R1,250 a night. Dogs welcome.", Picked: true)
        ],
        [
            new SceneStep(SceneStepKind.Ask, "Do you have a sea-view room?"),
            new SceneStep(SceneStepKind.Call, "ShowPage /rooms/garden")
            {
                Stage = SceneStage.Shown,
                Address = "seemeeu.example/rooms/garden",
                Foot = "1 call"
            },
            new SceneStep(SceneStepKind.Answer, "Dogs are welcome in the garden room.")
        ],
        "0 calls", "Seemeeu · ask us")
    {
        Slip = new ProductSlip("BOOKING REQUEST", "The garden room", [new SlipRow("Nights", "Friday and Saturday")],
            "Status", "Not sent yet")
    };

    /// <summary>The restaurant, a second scene with no slip.</summary>
    private static readonly ProductScene Restaurant = new(
        "wheatfieldkitchen.example", "Wheatfield Kitchen", ["MENU", "BOOK"],
        [new ProductRoom("/roast.webp", "Sunday lunch", "12:00 to 15:00")],
        [new SceneStep(SceneStepKind.Ask, "Are you open for lunch on Sunday?")],
        "0 calls", "Wheatfield · ask us");

    /// <summary>The window as the site gives it: grey glass, plain, with a title to ask about it by.</summary>
    private static IRenderedComponent<ProductWindow> Window(BunitContext ctx)
    {
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        IReadOnlyList<ProductScene> scenes = [Guesthouse, Restaurant];
        var grey = new GlassLook(GlassColour.Grey, 40, GlassType.Plain);
        return ctx.Render<ProductWindow>(p => p
            .Add(w => w.Title, "Three businesses at work")
            .Add(w => w.Label, "Three businesses' pages, each with its agent beside it")
            .Add(w => w.Scenes, scenes)
            .Add(w => w.Glass, grey));
    }

    /// <summary>Each scene is a business's page: its address, name, menu and cards, the one to pick marked.</summary>
    [Test]
    public void EachScene_IsABusinessesPage()
    {
        using var ctx = new BunitContext();

        var scenes = Window(ctx).FindAll(".product-scene");
        var first = scenes[0];

        Assert.Multiple(() =>
        {
            Assert.That(scenes, Has.Count.EqualTo(2));
            Assert.That(first.QuerySelector(".product-address")!.TextContent, Is.EqualTo("seemeeu.example"));
            Assert.That(first.QuerySelector(".product-head b")!.TextContent, Is.EqualTo("Seemeeu Guesthouse"));
            Assert.That(first.QuerySelectorAll(".product-head span").Select(s => s.TextContent),
                Is.EqualTo(new[] { "HOME", "ROOMS", "STAY" }));
            Assert.That(first.QuerySelectorAll(".product-room"), Has.Length.EqualTo(3));
            Assert.That(first.QuerySelector(".product-room.is-picked b")!.TextContent, Is.EqualTo("The garden room"));
            Assert.That(first.QuerySelector(".product-slip em")!.TextContent, Is.EqualTo("Not sent yet"));
            Assert.That(scenes[1].QuerySelector(".product-slip"), Is.Null);
        });
    }

    /// <summary>A scene's steps are written for the window's module to play, each with what it changes.</summary>
    [Test]
    public void TheScene_IsWrittenForTheModule()
    {
        using var ctx = new BunitContext();

        var steps = Window(ctx).FindAll(".product-scene")[0].QuerySelectorAll(".product-script i");

        Assert.Multiple(() =>
        {
            Assert.That(steps.Select(s => s.GetAttribute("data-kind")), Is.EqualTo(new[] { "ask", "call", "answer" }));
            Assert.That(steps[1].GetAttribute("data-stage"), Is.EqualTo("shown"));
            Assert.That(steps[1].GetAttribute("data-address"), Is.EqualTo("seemeeu.example/rooms/garden"));
            Assert.That(steps[1].GetAttribute("data-foot"), Is.EqualTo("1 call"));
            Assert.That(steps[2].TextContent, Is.EqualTo("Dogs are welcome in the garden room."));
        });
    }

    /// <summary>
    /// It is a glass panel like the others, in the glass it is given, a piece the visitor can ask about (Louis: "also
    /// an ask about this box"), and it calls no script of its own: the frame's scenes script plays every window, so a
    /// page that holds one needs nothing set up for it.
    /// </summary>
    [Test]
    public void TheWindow_IsAGlassPanel_PlayedByTheFrame()
    {
        using var ctx = new BunitContext();

        var window = Window(ctx).Find(".product-window");

        Assert.Multiple(() =>
        {
            Assert.That(window.HasAttribute("data-tilt"), Is.True);
            Assert.That(window.GetAttribute("data-glass"), Is.EqualTo("grey"));
            Assert.That(window.GetAttribute("data-glass-type"), Is.EqualTo("plain"));
            Assert.That(window.HasAttribute("data-piece"), Is.True);
            Assert.That(window.Id, Is.EqualTo("three-businesses-at-work"));
            Assert.That(ctx.JSInterop.Invocations, Is.Empty);
            Assert.That(File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
                    "src/web/LouisAgent.Web/Components/Site/SiteShell.razor.js")),
                Does.Contain("import { scenes } from \"../../js/scenes.js\";"));
        });
    }
}
