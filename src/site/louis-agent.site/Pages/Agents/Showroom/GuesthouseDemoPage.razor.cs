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
        new(1, "It shows the garden room: dogs stay there, so it offers that one instead."),
        new(2, "It shows the beach, and answers like a local about breakfast on the stoep."),
        new(3, "It reads the booking back before it sends anything."),
        new(4, "The booking goes to the owner only after the guest says yes."),
    ];

    /// <summary>
    /// Seemeeu Guesthouse's sample brand, a coastal boutique (F127.2, mockup 0): Atlantic, sand, salt and its slate
    /// ink, its labels in sea glass.
    /// </summary>
    private static readonly MockColours Brand = new("#0b3a4a", "#e9dcc3", "#fbf8f2", "#14262d", "#8fbfb9");

    /// <summary>
    /// What a custom agent could add for a guesthouse (Louis: "we could integrate with the booking system if they
    /// have one etc. Custom Agent").
    /// </summary>
    private static readonly DemoNote[] Notes =
    [
        new("Book into your booking system",
            "If you take bookings online, it sees which rooms are free and holds one in the system you already use, " +
            "instead of passing a request on."),
        new("Take the deposit",
            "It sends the guest a secure payment link, so the booking is confirmed the moment the deposit is paid."),
        new("Answer on WhatsApp", "The same agent, with the same facts, answers your guests on WhatsApp too."),
    ];

    /// <summary>The facts under its hero, each one its agent knows.</summary>
    private static readonly string[] Facts =
    [
        "Four rooms, breakfast included", "The beach, a five-minute walk", "Dogs in the garden room",
        "Wifi in every room"
    ];
}
