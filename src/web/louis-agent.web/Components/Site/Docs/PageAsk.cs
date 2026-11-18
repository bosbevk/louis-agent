namespace LouisAgent.Web.Components.Site.Docs;

/// <summary>
/// What a docs page offers to ask its site's agent about itself (F164.2): the button's words, and the question a press
/// puts to the agent in the visitor's words. The page the visitor reads goes with it, as with every message.
/// </summary>
/// <param name="Text">The button's words, such as "Ask about this page".</param>
/// <param name="Question">What a press asks the agent.</param>
public sealed record PageAsk(string Text, string Question);
