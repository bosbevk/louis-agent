using LouisAgent.Web.Components.Recorded;

namespace LouisAgent.Site.Pages.Agents.Showroom;

/// <summary>
/// The doctor's practice's demo (F128.2): a parent asks Graanveld Family Practice's agent about a fever, is not
/// advised, and books an appointment, each turn word for word what <c>src/site/recordings/record-showroom.sh</c> asked.
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
}
