using LouisAgent.Choices;
using LouisAgent.Identities.Guide;
using LouisAgent.Web.Components.Widget;

namespace LouisAgent.Site.Components.Guide;

/// <summary>
/// The site's guide (F109.3): the framework's <c>ChatWidget</c> wrapped once with the guide's identity and the site's
/// words, so nothing is configured twice (Louis: "why not in a component? also as well as layout"). It stands as
/// the bar at the foot of every page (F151.6, Louis: "we will use the same guide as in design chat widget
/// everywhere").
/// </summary>
public partial class ExpertChat
{
    /// <summary>Who answers: the guide, by the name the site's catalogue runs it under.</summary>
    private static string Identity => GuideIdentityName.LouisAgentGuide.Wire();
}
