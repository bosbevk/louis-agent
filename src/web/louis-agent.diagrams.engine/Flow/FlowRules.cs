using LouisAgent.Diagrams.Engine.Flow.Routing;
using LouisAgent.Diagrams.Engine.Flow.Rows;

namespace LouisAgent.Diagrams.Engine.Flow;

/// <summary>
/// The two rules a flow is laid out by, handed to the layout as one: how its arrows run, and which row each box
/// stands on.
/// </summary>
/// <param name="Arrows">Where an arrow bends and where it arrives.</param>
/// <param name="Rows">Which row a box stands on.</param>
/// <remarks>
/// Introduce Parameter Object (Refactoring.Guru), Louis's pick of three (F134.8): the two rules always travel
/// together, so a caller hands one value and can't pair the old arrows with the new rows by accident.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
public sealed record FlowRules(IArrowRule Arrows, IRowRule Rows)
{
    /// <summary>
    /// The rules every picture was laid out by before F134.8: arrows that join, boxes centred in their band.
    /// </summary>
    public static FlowRules Joined { get; } = new(new JoinedArrows(), new CentredRows());

    /// <summary>
    /// The rules a flow is laid out by since F134.8: each arrow at its own point, boxes level with what feeds them,
    /// so a line from one box to one box runs straight.
    /// </summary>
    public static FlowRules Straight { get; } = new(new ApartArrows(), new LevelRows());
}
