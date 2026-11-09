using LouisAgent.Web.Components.Recorded;

namespace LouisAgent.Site.Pages.Agents.Showroom;

/// <summary>
/// The law practice's demo (F128.2): someone asks Koringland Attorneys' agent about a will, is not advised, and books a
/// first consultation, each turn word for word what <c>src/site/recordings/record-showroom.sh</c> asked.
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
}
