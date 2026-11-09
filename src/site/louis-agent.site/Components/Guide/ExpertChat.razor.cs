using LouisAgent.Choices;
using LouisAgent.Identities.Guide;

namespace LouisAgent.Site.Components.Guide;

/// <summary>
/// The guide in the site's corner (F109.3): the framework's <c>ChatWidget</c> wrapped once with the guide's identity
/// and the site's words, so nothing is configured twice (Louis: "why not in a component? also as well as layout").
/// </summary>
public partial class ExpertChat
{
    /// <summary>Who answers: the guide, by the name the site's catalogue runs it under.</summary>
    private static string Identity => GuideIdentityName.LouisAgentGuide.Wire();
}
