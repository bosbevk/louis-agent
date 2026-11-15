using LouisAgent.Choices;
using LouisAgent.Identities.Guide;
using LouisAgent.Web.Components.Widget;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Components.Guide;

/// <summary>
/// The guide in the site's corner (F109.3): the framework's <c>ChatWidget</c> wrapped once with the guide's identity
/// and the site's words, so nothing is configured twice (Louis: "why not in a component? also as well as layout").
/// </summary>
public partial class ExpertChat
{
    /// <summary>Who answers: the guide, by the name the site's catalogue runs it under.</summary>
    private static string Identity => GuideIdentityName.LouisAgentGuide.Wire();

    /// <summary>How the guide stands: in its corner, or as the bar at the foot of a showroom page (F134.3).</summary>
    [Parameter]
    public WidgetLayout Layout { get; set; } = WidgetLayout.Corner;
}
