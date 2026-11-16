using LouisAgent.Web.Components.Site.Showcase;

namespace LouisAgent.Site.Components.Landing.Offer;

/// <summary>
/// What the landing's running tape says, as the design page has it: what the three businesses' agents did, a call, a
/// refusal, a time and a cost, each at a glance.
/// </summary>
public static class LandingTape
{
    /// <summary>The items, in the order they run.</summary>
    public static IReadOnlyList<TapeItem> Items { get; } =
    [
        new("ShowPage", "/rooms/garden"), new("Took a table for five"), new("Refused to give", "legal advice"),
        new("SendLead once, on a yes"), new("Answered at", "22:04"), new("Read the booking back"),
        new("ShowCard", "Booking request"), new("Cost", "US$0.005")
    ];
}
