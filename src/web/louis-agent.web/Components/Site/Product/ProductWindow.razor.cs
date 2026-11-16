using LouisAgent.Web.Components.Site.Pieces;
using LouisAgent.Web.Components.Site.Product.Page;
using LouisAgent.Web.Components.Site.Product.Scene;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Product;

/// <summary>
/// Shows a business's own website with its agent working beside it: the visitor asks, the agent calls its tools,
/// opens the right card, draws the request and sends it on a yes, one business after another.
/// </summary>
/// <remarks>
/// The design page's product window (FX163, Louis: "this could be the example", "then put each of these inside"),
/// its scene taken as a list of steps the site gives, so the framework plays any business's talk and names none.
/// The markup writes the steps; the frame's scenes script, which it brings to every page as it does the lens, reads
/// them and plays them in a loop.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class ProductWindow
{
    /// <summary>What the window shows, read aloud, since its scene plays for the eye.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = "";

    /// <summary>The businesses, played in turn.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<ProductScene> Scenes { get; set; } = [];

    /// <summary>Whether the theme's lens bends what is behind the window; on by default.</summary>
    [Parameter] public bool Lens { get; set; } = true;

    /// <summary>Whether the window leans towards the pointer, as far as the theme says; on by default.</summary>
    [Parameter] public bool Tilt { get; set; } = true;

    /// <summary>Whether the window shows its reflection under it; off by default.</summary>
    [Parameter] public bool Reflect { get; set; }

    /// <summary>Whether the window's glass ripples what is behind it like water; on by default.</summary>
    [Parameter] public bool Water { get; set; } = true;

    /// <summary>What the window tells the lens: nothing while it is on, so only a switched-off lens is written.</summary>
    private string? LensSwitch => PaneSwitch.Lens(Lens);

    /// <summary>A scene, the first one showing until the module turns to the next.</summary>
    private static string SceneClass(int index) => index == 0 ? "product-scene is-on" : "product-scene";

    /// <summary>A card, marked when it is the one the agent opens.</summary>
    private static string RoomClass(ProductRoom room) => room.Picked ? "product-room is-picked" : "product-room";

    /// <summary>What a step is, as the module reads it.</summary>
    private static string KindOf(SceneStep step) => step.Kind switch
    {
        SceneStepKind.Ask => "ask",
        SceneStepKind.Think => "think",
        SceneStepKind.Call => "call",
        SceneStepKind.Answer => "answer",
        SceneStepKind.Offer => "offer",
        SceneStepKind.Press => "press",
        _ => "wait"
    };

    /// <summary>How far a step brings the page, as the module reads it; none for a step that changes nothing.</summary>
    private static string? StageOf(SceneStep step) => step.Stage switch
    {
        SceneStage.Shown => "shown",
        SceneStage.Card => "card",
        SceneStage.Sent => "sent",
        _ => null
    };
}
