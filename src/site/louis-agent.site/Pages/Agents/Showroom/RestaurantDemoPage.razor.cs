using LouisAgent.Web.Components.Recorded;

namespace LouisAgent.Site.Pages.Agents.Showroom;

/// <summary>
/// The restaurant's demo (F128.2): a family asks Wheatfield Kitchen's agent about Sunday lunch and books, each turn
/// word for word what <c>src/site/recordings/record-showroom.sh</c> asked.
/// </summary>
public partial class RestaurantDemoPage
{
    /// <summary>The conversation, a turn at a time.</summary>
    private static readonly RecordedTurn[] Conversation =
    [
        new("Hi, are you open for lunch on Sunday? We are a family of five, with two small kids.",
            "recordings/showroom/wheatfield-kitchen-1.sse"),
        new("Lovely. Could we book a table for Sunday at half past twelve?",
            "recordings/showroom/wheatfield-kitchen-2.sse"),
        new("Sam Botha, and my email is sam@example.com", "recordings/showroom/wheatfield-kitchen-3.sse"),
        new("Yes please, send it.", "recordings/showroom/wheatfield-kitchen-4.sse"),
    ];
}
