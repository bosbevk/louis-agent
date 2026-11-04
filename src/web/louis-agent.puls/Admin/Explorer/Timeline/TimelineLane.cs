namespace LouisAgent.Puls.Admin.Explorer.Timeline;

/// <summary>One lane of a turn's timeline, Puls's or the specialist's, with its requests' bars.</summary>
public sealed record TimelineLane(string Label, bool Specialist, IReadOnlyList<TimelineBar> Bars);
