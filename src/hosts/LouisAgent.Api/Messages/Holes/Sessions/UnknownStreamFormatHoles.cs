namespace LouisAgent.Api.Messages.Holes.Sessions;

/// <summary>A stream format that isn't one of ours, and the ones that are.</summary>
public sealed record UnknownStreamFormatHoles(string? Format, string Supported);
