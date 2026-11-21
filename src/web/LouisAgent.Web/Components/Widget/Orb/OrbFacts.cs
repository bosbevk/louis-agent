namespace LouisAgent.Web.Components.Widget.Orb;

/// <summary>What the chat says of itself, which is all the orb's state is read from.</summary>
/// <param name="Streaming">An answer is coming in.</param>
/// <param name="HasWords">The answer coming in has words already.</param>
/// <param name="Showing">The agent showed a place on the page a moment ago.</param>
/// <param name="Heard">The visitor is writing, or holds a piece of the page they picked.</param>
public sealed record OrbFacts(bool Streaming, bool HasWords, bool Showing, bool Heard);
