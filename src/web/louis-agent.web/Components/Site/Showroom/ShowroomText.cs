namespace LouisAgent.Web.Components.Site.Showroom;

/// <summary>
/// The words the showroom shows around a site's demos (F127.1), so each site can show them in its own language.
/// English by default; a site registers its own copy, as it does for <c>ChatText</c>.
/// </summary>
public sealed record ShowroomText
{
    /// <summary>The showroom's name, on the pill beside the brand.</summary>
    public string Demos { get; init; } = "Demos";

    /// <summary>The rail's first group: what an agent would do for a visitor's business.</summary>
    public string ForYourBusiness { get; init; } = "For your business";

    /// <summary>The rail's second group: agents doing work.</summary>
    public string AgentsAtWork { get; init; } = "Agents at work";

    /// <summary>The way out of the showroom, back to the rest of the site.</summary>
    public string BackToSite { get; init; } = "Back to the site";

    /// <summary>The name the rail goes by for a screen reader.</summary>
    public string AllDemos { get; init; } = "All demos";

    /// <summary>The heading over a demo's cues.</summary>
    public string WhatToWatch { get; init; } = "What to watch";

    /// <summary>The demo's place on its stage, as the guide's tour names it.</summary>
    public string TheDemo { get; init; } = "The demo";

    /// <summary>The offer's place on a demo's stage, as the guide's tour names it.</summary>
    public string TheOffer { get; init; } = "Ask for yours";
}
