using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Components.Site.Showroom.Mock;

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

    /// <summary>
    /// Seemeeu Guesthouse's sample brand, a coastal boutique (F127.2, mockup 0): Atlantic, sand, salt and its slate
    /// ink, its labels in sea glass.
    /// </summary>
    private static readonly MockColours Brand = new("#0b3a4a", "#e9dcc3", "#fbf8f2", "#14262d", "#8fbfb9");

    /// <summary>The facts under its hero, each one its agent knows.</summary>
    private static readonly string[] Facts =
    [
        "Four rooms, breakfast included", "The beach, a five-minute walk", "Dogs in the garden room",
        "Wifi in every room"
    ];
}
