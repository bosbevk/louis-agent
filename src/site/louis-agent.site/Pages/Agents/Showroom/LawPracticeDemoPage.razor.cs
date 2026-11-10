using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Components.Site.Showroom.Mock;
using LouisAgent.Web.Components.Site.Showroom.Stage;

namespace LouisAgent.Site.Pages.Agents.Showroom;

/// <summary>
/// The law practice's demo (F128.2, in the showroom since F127.1): someone asks Koringland Attorneys' agent about a
/// will, is not advised, and books a first consultation, each turn word for word what
/// <c>src/site/recordings/record-showroom.sh</c> asked, on a mock of the practice's own website in its sample brand.
/// </summary>
public partial class LawPracticeDemoPage
{
    /// <summary>The conversation, a turn at a time.</summary>
    private static readonly RecordedTurn[] Conversation =
    [
        new("Good morning. I need to change my will after my divorce. What should I put in it?",
            "recordings/showroom/koringland-attorneys-1.sse"),
        new("Okay, I would like a consultation then, on a Thursday morning if possible.",
            "recordings/showroom/koringland-attorneys-2.sse"),
        new("Anna de Villiers, anna@example.com", "recordings/showroom/koringland-attorneys-3.sse"),
        new("Yes, please send it.", "recordings/showroom/koringland-attorneys-4.sse"),
    ];

    /// <summary>What to watch for, each lit as the play reaches its turn.</summary>
    private static readonly DemoCue[] WhatToWatch =
    [
        new(1, "It gives no legal advice: it shows its wills page and offers a first consultation."),
        new(2, "It asks for one thing at a time, starting with a name."),
        new(3, "It reads the request back before it sends anything."),
        new(4, "The request goes to the practice only after the client says yes."),
    ];

    /// <summary>
    /// Koringland Attorneys' sample brand, an established country firm (F127.2, mockup 0): midnight, vellum,
    /// parchment and its ink, its labels and its rule in brass.
    /// </summary>
    private static readonly MockColours Brand = new("#14213d", "#f6f2ea", "#f6f2ea", "#1b2233", "#b08d57");

    /// <summary>What a custom agent could add for a law practice.</summary>
    private static readonly DemoNote[] Notes =
    [
        new("Book into the diaries",
            "It sees when the attorneys are free and books the first consultation straight into the diary."),
        new("Collect the papers first",
            "It asks for the ID and the documents before the meeting and files them with the matter."),
        new("Call back when it's urgent",
            "An arrest or a court date this week goes straight to an attorney's phone, for a call back today."),
    ];

    /// <summary>The facts under its hero, each one its agent knows.</summary>
    private static readonly string[] Facts =
        ["Monday to Friday", "At the office or by video call", "English and Afrikaans", "A first consultation R650"];
}
