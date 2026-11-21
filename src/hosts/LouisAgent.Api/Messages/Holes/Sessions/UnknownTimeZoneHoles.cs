namespace LouisAgent.Api.Messages.Holes.Sessions;

/// <summary>The time zone a session asked for that has no IANA name.</summary>
public sealed record UnknownTimeZoneHoles(string TimeZone);
