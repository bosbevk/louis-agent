using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Components.Site.Showroom.Mock;
using LouisAgent.Web.Components.Site.Showroom.Stage;

namespace LouisAgent.Site.Pages.Agents.Showroom;

/// <summary>
/// The guesthouse's demo (F127.1, in place of the doctor's practice; Louis: "we should just change doctor to something
/// else"): a couple asks Seemeeu Guesthouse's agent about a weekend with their dog and books, each turn word for word
/// what <c>src/site/recordings/record-showroom.sh</c> asked, on a mock of the guesthouse's own website in its sample
/// brand.
/// </summary>
public partial class GuesthouseDemoPage
{
    /// <summary>The conversation, a turn at a time.</summary>
    private static readonly RecordedTurn[] Conversation =
    [
        new("Hi, do you have a sea-view room for this Friday and Saturday night? We are two, and we would like to " +
            "bring our dog.", "recordings/showroom/seemeeu-guesthouse-1.sse"),
        new("Then the garden room, please. Is the beach far, and what time is breakfast?",
            "recordings/showroom/seemeeu-guesthouse-2.sse"),
        new("Marike Botha, marike@example.com", "recordings/showroom/seemeeu-guesthouse-3.sse"),
        new("Yes please, send it.", "recordings/showroom/seemeeu-guesthouse-4.sse"),
    ];

    /// <summary>What to watch for, each lit as the play reaches its turn.</summary>
    private static readonly DemoCue[] WhatToWatch =
    [
        new(1, "It knows the house: dogs stay in the garden room, so it offers that one instead."),
        new(2, "It answers like a local: the beach, breakfast on the stoep, the park nearby."),
        new(4, "The booking goes to the owner only after the guest says yes."),
    ];

    /// <summary>Seemeeu Guesthouse's sample brand: a deep sea blue, sand, and dark slate ink.</summary>
    private static readonly MockColours Brand = new("#0e4f63", "#f7f1e3", "#fbf8f1", "#1c2b33");

    /// <summary>Its website's menu.</summary>
    private static readonly string[] Menu = ["Rooms", "Breakfast", "The beach", "Find us"];
}
