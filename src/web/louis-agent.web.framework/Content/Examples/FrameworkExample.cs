using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Tones;
using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Web.Framework.Content.Examples;

/// <summary>
/// One agent the framework already runs (F160.1, step 12): its name, the identity that makes it, by its folder in the
/// repo, the icon and colour it wears in the page's picture, what it is in a line, and what it does, a line each, from
/// its identity's and its packs' own summaries.
/// </summary>
/// <remarks>The identity's folder is the example's proof: the page's test finds it on the disk.</remarks>
public sealed record FrameworkExample(
    string Name,
    string Identity,
    DiagramIcon Icon,
    DiagramTone Tone,
    string Line,
    IReadOnlyList<Term> What);
