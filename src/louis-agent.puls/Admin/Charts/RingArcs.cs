namespace LouisAgent.Puls.Admin.Charts;

/// <summary>One arc of a ring: which part it is (its index in the parts), its length out of the circumference of 100, and its stroke-dashoffset.</summary>
public sealed record RingArc(int Index, double Length, double Offset);

/// <summary>
/// The arcs of a ring (F18 step 6a.3b, out of RunExplorer's <c>Ring</c>): a circle's stroke-dasharray, with a circumference of 100 so a length is a percentage, each arc starting where the one
/// before ended, from twelve o'clock (an offset of 25). A part of 0 draws no arc but keeps its index, so its colour and title stay its own; when every part is 0 there are no arcs, only
/// the track.
/// </summary>
public static class RingArcs
{
    /// <summary>The radius of a circle whose circumference is <see cref="Circumference"/>.</summary>
    public const double Radius = 15.915494, Circumference = 100;

    private const double Twelve = 25;

    public static IReadOnlyList<RingArc> Of(IReadOnlyList<double> parts)
    {
        double total = parts.Sum();
        if (total <= 0) return [];
        var arcs = new List<RingArc>();
        double offset = Twelve;
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] <= 0) continue;
            double length = parts[i] / total * Circumference;
            arcs.Add(new RingArc(i, length, offset));
            offset -= length;
        }

        return arcs;
    }
}
