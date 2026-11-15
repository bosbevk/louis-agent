namespace LouisAgent.Web.Components.Widget.Bar;

/// <summary>
/// What a site's guide offers to be asked (F134.3): the questions its bar shows as chips while the guide waits.
/// </summary>
/// <remarks>
/// A site registers it once, as it registers its places and its words, so the framework names no site. A chip is a
/// shortcut and never the only way in: the box is always there, and the chips say so (F134's rule 3).
/// </remarks>
/// <param name="Questions">Each chip's words, which are the question it asks.</param>
public sealed record AgentSuggestions(IReadOnlyList<string> Questions);
