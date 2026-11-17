using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Web.Framework.Content.Features;

/// <summary>
/// One area of the framework the Features page tells by (F160.1, step 11): its section's address, its name, the icon
/// and colour it wears in the page's picture at a glance, a tone an area or the picture's own violet with none, and
/// what its entries are counted as, features, or rules for how the code is written.
/// </summary>
public sealed record FeatureArea(
    string Id,
    string Name,
    DiagramIcon Icon,
    DiagramTone? Tone = null,
    string Counts = "features");
