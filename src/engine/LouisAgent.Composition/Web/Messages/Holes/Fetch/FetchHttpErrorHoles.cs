namespace LouisAgent.Composition.Web.Messages.Holes.Fetch;

/// <summary>The page that answered with an error, its status and the server's reason.</summary>
public sealed record FetchHttpErrorHoles(string Url, int Status, string? Reason);
