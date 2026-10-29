using LouisAgent.Puls.Admin.Explorer.Tree;

namespace LouisAgent.Puls.Admin.Explorer.Timeline;

/// <summary>
/// One request's bar on a turn's timeline: its place in its lane, and where it starts and how wide it is, as
/// percentages of the axis written for a style.
/// </summary>
public sealed record TimelineBar(RequestNode Request, int I, string Left, string Width);
