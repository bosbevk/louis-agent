namespace LouisAgent.Composition.Web.Messages.Holes.Fetch;

/// <summary>The page that isn't text, and what kind of file it is.</summary>
public sealed record FetchNotTextHoles(string Url, string MediaType);
