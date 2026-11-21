using LouisAgent.Web.Components.Site.Ticker;

namespace LouisAgent.Site.Components.Landing.Offer;

/// <summary>
/// What the landing's running tape says, as the design page has it: what the three businesses' agents did, a call, a
/// refusal, a time and a cost, each at a glance, in plain words a business owner reads, never a tool's name (FX205).
/// </summary>
public static class LandingTape
{
    /// <summary>The items, in the order they run.</summary>
    public static IReadOnlyList<TapeItem> Items { get; } =
    [
        new("Showed the", "garden room"), new("Took a table for five"), new("Refused to give", "legal advice"),
        new("Passed a booking on, after a yes"), new("Answered at", "22:04"), new("Read the booking back"),
        new("Showed the", "booking request"), new("Cost", "R0.09")
    ];
}
