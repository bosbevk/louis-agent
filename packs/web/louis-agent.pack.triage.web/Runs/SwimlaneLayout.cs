namespace LouisAgent.Packs.Triage.Web.Runs;

/// <summary>One step on the swimlanes: which step it is, its agent's lane, and where its dot sits.</summary>
/// <param name="Step">The step's place in the run, from 0.</param>
/// <param name="Lane">Its agent's lane, in the stage's order.</param>
/// <param name="X">The dot's centre across the picture.</param>
/// <param name="Y">The dot's centre down the picture.</param>
public sealed record LaneDot(int Step, int Lane, int X, int Y);

/// <summary>
/// Where the steps of a run go on the swimlanes (F122): a lane per agent in the stage's order, each step a dot in its
/// agent's lane, one column further right than the step before, and the line that joins them.
/// </summary>
public static class SwimlaneLayout
{
    /// <summary>Room for the agents' names at the left of each lane.</summary>
    public const int LabelWidth = 130;

    /// <summary>The room each step takes across.</summary>
    public const int ColumnWidth = 44;

    /// <summary>One lane's height.</summary>
    public const int LaneHeight = 32;

    /// <summary>The dots of the steps played so far, by the agent of each.</summary>
    public static IReadOnlyList<LaneDot> Dots(IEnumerable<string> agents) =>
        agents.Select((agent, step) =>
        {
            int lane = Math.Max(0, AgentWords.Stage.ToList().IndexOf(agent));
            return new LaneDot(step, lane, LabelWidth + ColumnWidth / 2 + step * ColumnWidth,
                LaneHeight / 2 + lane * LaneHeight);
        }).ToList();

    /// <summary>The line through the dots in order, as an SVG path; empty for no dots.</summary>
    public static string Line(IReadOnlyList<LaneDot> dots) =>
        string.Join(" ", dots.Select((dot, k) => $"{(k == 0 ? "M" : "L")}{dot.X} {dot.Y}"));

    /// <summary>The picture's width for a run of this many steps, so the dots stay put as steps arrive.</summary>
    public static int Width(int steps) => LabelWidth + steps * ColumnWidth;

    /// <summary>The picture's height: a lane per agent.</summary>
    public static int Height => AgentWords.Stage.Count * LaneHeight;
}
