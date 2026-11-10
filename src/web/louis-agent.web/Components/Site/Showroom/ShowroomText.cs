using LouisAgent.Choices;

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

    /// <summary>The rail's name for a screen reader, and a demo's way back to all of them.</summary>
    public string AllDemos { get; init; } = "All demos";

    /// <summary>A mock website's chat folds to its head.</summary>
    public string Minimise { get; init; } = "Minimise";

    /// <summary>A mock website's chat covers the website.</summary>
    public string Maximise { get; init; } = "Maximise";

    /// <summary>A minimised or maximised chat goes back to its corner.</summary>
    public string Restore { get; init; } = "Restore";

    /// <summary>The heading over a demo's cues.</summary>
    public string WhatToWatch { get; init; } = "What to watch";

    /// <summary>The demo's place on its stage, as the guide's tour names it.</summary>
    public string TheDemo { get; init; } = "The demo";

    /// <summary>The offer's place on a demo's stage, as the guide's tour names it.</summary>
    public string TheOffer { get; init; } = "Ask for yours";

    /// <summary>A group of demos by its name in the site's words, for the rail and the start alike.</summary>
    /// <remarks>A switch on the group, one arm each (ADR 0051).</remarks>
    public string NameOf(DemoGroup group) => group switch
    {
        DemoGroup.ForYourBusiness => ForYourBusiness,
        DemoGroup.AgentsAtWork => AgentsAtWork,
        _ => group.Wire(),
    };
}
