using LouisAgent.Web.Components.Site.Showcase;

namespace LouisAgent.Site.Components.Landing.Offer;

/// <summary>What the agent does for a business, as four things it calls, each with its small film.</summary>
public partial class WhatItDoes
{
    /// <summary>The four things, as the design page names them (ADR 0125).</summary>
    private static readonly IReadOnlyList<Feature> Features =
    [
        new(FeatureFilm.Show, "It takes them to the right place.", "A guest wants a sea view and is bringing a dog. Only the garden room takes dogs, so that is the room it opens.", "ShowPage"),
        new(FeatureFilm.Draw, "It shows instead of tells.", "The booking comes back as a clear card, so nobody has to read through a wall of text.", "ShowCard"),
        new(FeatureFilm.Ask, "It asks before it sends.", "Nothing leaves the page until the visitor says yes. Then the request reaches you, once.", "Offer, then SendLead"),
        new(FeatureFilm.Flip, "It shows its work.", "Turn any answer over to see what it did, how long it took and what it cost.", "the usage ledger")
    ];
}
