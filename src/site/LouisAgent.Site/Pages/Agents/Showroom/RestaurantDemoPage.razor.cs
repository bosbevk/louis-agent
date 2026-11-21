using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Components.Site.Showroom.Mock;

namespace LouisAgent.Site.Pages.Agents.Showroom;

/// <summary>
/// The restaurant's demo (F128.2, in the showroom since F127.1): a family asks Wheatfield Kitchen's agent about Sunday
/// lunch and books, each turn word for word what <c>src/site/recordings/record-showroom.sh</c> asked, on a mock of the
/// restaurant's own website in its sample brand.
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

    /// <summary>
    /// Wheatfield Kitchen's sample brand, a Swartland farm bistro (F127.2, mockup 0): olive, linen and its brown ink,
    /// its labels in harvest gold and its mark in wheat.
    /// </summary>
    private static readonly MockColours Brand = new("#2c4a34", "#f7f0e1", "#f7f0e1", "#2b2418", "#9c7a2f");

    /// <summary>The facts under its hero, each one its agent knows.</summary>
    private static readonly string[] Facts =
    [
        "Lunch Tuesday to Sunday", "Dinner Thursday to Saturday", "Sixty seats, on the stoep too", "Wheelchair access"
    ];
}
