using LouisAgent.Web.Components.Site.Showcase;

namespace LouisAgent.Site.Components.Landing.Offer;

/// <summary>What the agent does for a business, as four things it calls, each with its small film.</summary>
public partial class WhatItDoes
{
    /// <summary>The four things, as the design page names them (ADR 0125).</summary>
    private static readonly IReadOnlyList<Feature> Features =
    [
        new(FeatureFilm.Show, "It opens the right page.", "A guest asks for a sea view and a dog. Dogs stay in the garden room, so that is the room it shows.", "ShowPage"),
        new(FeatureFilm.Draw, "It draws what you asked for.", "No wall of text. The booking comes back as a card you can read at a glance.", "ShowCard"),
        new(FeatureFilm.Ask, "It asks before it sends.", "Nothing leaves the page until the guest says yes. Then you get the request, once.", "Offer, then SendLead"),
        new(FeatureFilm.Flip, "It shows its work.", "Turn any answer over. You see what it called, how long it took and what it cost.", "the usage ledger")
    ];
}
