namespace LouisAgent.Web.Components.Widget.Bar.Opening;

/// <summary>
/// What the bar knows just now, for whether its box is open (FX172, Louis: "the agent must start closed, then only when
/// it request a answer from you or you from him", his pick A of three). The bar starts as the orb alone, and opens
/// while one side needs the other: the visitor holds it (pressed the orb, or is in the box), is writing, or picked a
/// piece of the page; or the guide is answering, or an answer stands over the bar.
/// </summary>
/// <param name="Held">The visitor pressed the orb, or is in the box.</param>
/// <param name="Answering">The guide is answering.</param>
/// <param name="Writing">The box holds words not yet sent.</param>
/// <param name="Picked">The visitor picked a piece of the page to ask about.</param>
/// <param name="Answered">The guide's answer, or the whole talk, stands over the bar.</param>
public sealed record BarFacts(bool Held, bool Answering, bool Writing, bool Picked, bool Answered)
{
    /// <summary>Whether the box is open: any one need opens it.</summary>
    public bool Open => Held || Answering || Writing || Picked || Answered;
}
