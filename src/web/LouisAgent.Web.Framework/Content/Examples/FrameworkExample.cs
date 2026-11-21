using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Tones;
using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Web.Framework.Content.Examples;

/// <summary>
/// One agent the framework already runs (F164.1, step 12): its name, the identity that makes it, by where it lives
/// (a folder here, or a path in another repository of ours, F166.4), the icon and colour it wears in the page's
/// picture, what it is in a line, and what it does, a line each, from its identity's and its packs' own summaries.
/// </summary>
/// <remarks>
/// The identity's home is the example's proof: the page's test finds a folder here on the disk, and one elsewhere by
/// its repository of ours (ADR 0100).
/// </remarks>
public sealed record FrameworkExample(
    string Name,
    ExampleHome Identity,
    DiagramIcon Icon,
    DiagramTone Tone,
    string Line,
    IReadOnlyList<Term> What);
