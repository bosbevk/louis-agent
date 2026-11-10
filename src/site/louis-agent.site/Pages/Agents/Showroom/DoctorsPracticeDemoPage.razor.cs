using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Components.Site.Showroom.Mock;
using LouisAgent.Web.Components.Site.Showroom.Stage;

namespace LouisAgent.Site.Pages.Agents.Showroom;

/// <summary>
/// The doctor's practice's demo (F128.2, in the showroom since F127.1): a parent asks Graanveld Family Practice's agent
/// about a fever, is not advised, and books an appointment, each turn word for word what
/// <c>src/site/recordings/record-showroom.sh</c> asked, on a mock of the practice's own website in its sample brand.
/// </summary>
public partial class DoctorsPracticeDemoPage
{
    /// <summary>The conversation, a turn at a time.</summary>
    private static readonly RecordedTurn[] Conversation =
    [
        new("Hi, my son has had a fever since yesterday. What medicine should I give him?",
            "recordings/showroom/graanveld-family-practice-1.sse"),
        new("Can he see a doctor tomorrow morning?", "recordings/showroom/graanveld-family-practice-2.sse"),
        new("Jacques Louw, jacques@example.com, it is for my son Pieter.",
            "recordings/showroom/graanveld-family-practice-3.sse"),
        new("Yes please.", "recordings/showroom/graanveld-family-practice-4.sse"),
    ];

    /// <summary>What to watch for, each lit as the play reaches its turn.</summary>
    private static readonly DemoCue[] WhatToWatch =
    [
        new(1, "It gives no medical advice, and says when to call an ambulance."),
        new(2, "It knows the practice: the hours, the doctors and what to bring."),
        new(4, "The request goes to reception only after the parent says yes, with no more about the child's health."),
    ];

    /// <summary>Graanveld Family Practice's sample brand: a calm teal, a pale mint, and dark green-grey ink.</summary>
    private static readonly MockColours Brand = new("#1d6b66", "#f2f7f6", "#f4f8f7", "#1e2b2a");

    /// <summary>Its website's menu.</summary>
    private static readonly string[] Menu = ["Our doctors", "Appointments", "Medical aid", "Find us"];
}
