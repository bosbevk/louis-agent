namespace LouisAgent.Web.Components.Widget.Door;

/// <summary>
/// One guide for the whole site (FX135, Louis: "the agentsession must be for the whole site"): every corner widget of
/// an identity takes the same <see cref="KeptGuide"/>, so a move to a page in another layout carries the talk on. A
/// demo's own window keeps its own session.
/// </summary>
/// <remarks>
/// Registry (Fowler, PoEAA): one small shared object the site registers once, as it does <see cref="GuideDoor"/>; a
/// site without it gets a widget that keeps its state to itself, as before.
/// </remarks>
public sealed class GuideKeeper
{
    private readonly Dictionary<string, KeptGuide> _guides = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The guide of this identity, kept from the first widget that asked.</summary>
    public KeptGuide For(string identity)
    {
        if (!_guides.TryGetValue(identity, out var guide)) _guides[identity] = guide = new KeptGuide();
        return guide;
    }
}
