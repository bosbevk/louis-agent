using LouisAgent.Web.Components.Site.Showcase;

namespace LouisAgent.Site.Components.Landing.Offer;

/// <summary>What the agent does for a business, as four things it calls, each with its small film.</summary>
public partial class WhatItDoes
{
    /// <summary>The four things, as the design page names them (ADR 0125).</summary>
    private static readonly IReadOnlyList<Feature> Features =
    [
        new(FeatureFilm.Show, "It takes them to the right place.", "A guest wants a sea view and is bringing a dog. Only the garden room takes dogs, so that is the room it opens.", "ShowPage"),
        new(FeatureFilm.Draw, "It shows the booking as a card.", "The request comes back as a clear card, not a wall of text.", "ShowCard"),
        new(FeatureFilm.Ask, "It asks before it sends.", "Nothing leaves the page until the visitor says yes. Then the request reaches you, once.", "Offer, then SendLead"),
        new(FeatureFilm.Flip, "It shows what each answer cost.", "Turn any answer over to see what it did, how long it took and what it cost.", "the usage ledger")
    ];
}
