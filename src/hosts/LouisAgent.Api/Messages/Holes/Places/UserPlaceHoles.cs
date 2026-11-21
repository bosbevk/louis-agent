namespace LouisAgent.Api.Messages.Holes.Places;

/// <summary>Where the user is, said once in the first message: their location and their time zone.</summary>
public sealed record UserPlaceHoles(string Location, string TimeZone);
