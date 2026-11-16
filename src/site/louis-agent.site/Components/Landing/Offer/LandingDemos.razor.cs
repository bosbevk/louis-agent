using LouisAgent.Web.Components.Site.Showcase;

namespace LouisAgent.Site.Components.Landing.Offer;

/// <summary>
/// The demos on the landing: three made-up businesses, fanned out as photographs that open each demo.
/// </summary>
public partial class LandingDemos
{
    /// <summary>The three businesses, as the design page fans them (ADR 0125).</summary>
    private static readonly IReadOnlyList<Shot> Shots =
    [
        new("A law practice", "It knows what it must not answer.", "images/showroom/koringland-desk.webp", "/law-practice-demo"),
        new("A guesthouse", "It opens the room that fits, not the one that was asked for.", "images/showroom/seemeeu-sea-view.webp", "/guesthouse-demo"),
        new("A restaurant", "It answers at ten at night.", "images/showroom/wheatfield-stoep.webp", "/restaurant-demo")
    ];
}
