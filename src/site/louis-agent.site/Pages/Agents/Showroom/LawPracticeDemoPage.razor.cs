using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Components.Site.Showroom.Mock;

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

    /// <summary>
    /// Koringland Attorneys' sample brand, an established country firm (F127.2, mockup 0): midnight, vellum,
    /// parchment and its ink, its labels and its rule in brass.
    /// </summary>
    private static readonly MockColours Brand = new("#14213d", "#f6f2ea", "#f6f2ea", "#1b2233", "#b08d57");

    /// <summary>The facts under its hero, each one its agent knows.</summary>
    private static readonly string[] Facts =
        ["Monday to Friday", "At the office or by video call", "English and Afrikaans", "A first consultation R650"];
}
