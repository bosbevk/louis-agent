namespace LouisAgent.Api.Messages.Holes.Door;

/// <summary>How many conversations a day the open door gives one address, told to a visitor who has had them.</summary>
public sealed record OpenDoorCapReachedHoles(int Cap);
