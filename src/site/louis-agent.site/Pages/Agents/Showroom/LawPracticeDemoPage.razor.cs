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
        new(1, "It gives no legal advice: it explains what a first consultation is instead."),
        new(2, "It never promises a time: the practice confirms the consultation."),
        new(4, "The request goes to the practice only after the client says yes."),
    ];

    /// <summary>Koringland Attorneys' sample brand: a deep navy, warm paper, and near-black ink.</summary>
    private static readonly MockColours Brand = new("#1f2f4f", "#f4efe4", "#f7f5f0", "#1d2433");

    /// <summary>Its website's menu.</summary>
    private static readonly string[] Menu = ["Wills and estates", "Property", "Family law", "Contact"];
}
