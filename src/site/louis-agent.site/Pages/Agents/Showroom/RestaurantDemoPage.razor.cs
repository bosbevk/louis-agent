using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Components.Site.Showroom.Mock;
using LouisAgent.Web.Components.Site.Showroom.Stage;

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

    /// <summary>What to watch for, each lit as the play reaches its turn.</summary>
    private static readonly DemoCue[] WhatToWatch =
    [
        new(1, "It answers at ten at night, and shows the children's menu for the two small kids."),
        new(2, "It never promises a table: it takes the request for the owner to confirm."),
        new(3, "It reads the request back before it sends anything."),
        new(4, "It sends the details only after the family says yes."),
    ];

    /// <summary>Wheatfield Kitchen's sample brand: a farm green, cream, and dark brown ink.</summary>
    private static readonly MockColours Brand = new("#2f5d45", "#f6f1e7", "#f6f1e7", "#2b2418");

    /// <summary>What a custom agent could add for a restaurant.</summary>
    private static readonly DemoNote[] Notes =
    [
        new("Book into your table system",
            "If you use a booking system, it books the table straight in and sends the confirmation, instead of " +
            "passing a request on."),
        new("Know today's specials",
            "It reads the day's specials and what has sold out from your kitchen, so it never offers what isn't " +
            "there."),
        new("Remind your guests",
            "It reminds the guests the day before and asks them to confirm, so fewer tables stand empty."),
    ];

    /// <summary>The facts under its hero, each one its agent knows.</summary>
    private static readonly string[] Facts =
    [
        "Lunch Tuesday to Sunday", "Dinner Thursday to Saturday", "Sixty seats, on the stoep too", "Wheelchair access"
    ];
}
