namespace LouisAgent.Composition.Web.Messages.Holes.Redirects;

/// <summary>How many redirects a fetch follows at most.</summary>
public sealed record FetchTooManyRedirectsHoles(int Count);
