using LouisAgent.Diagrams.Icons;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Icons;

/// <summary>
/// Draws one line icon on a page (F134.1), in the colour and at the size of the words around it, the same on every
/// system. A demo's sign used to be an emoji, which Windows draws black and uneven (Louis: "this looks off and those
/// icons").
/// </summary>
/// <remarks>
/// One set of icons for the whole framework (Louis's pick of three: one set, in the diagrams project, over a second
/// set here, or renaming the first). The strokes are the diagrams' own <see cref="IconPaths"/>, Tabler Icons copied
/// in as data, so a page and a diagram never hold two copies of a drawing and there is no package and no icon font.
/// This component is only how a page draws one.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0030-build-or-adopt.md">ADR 0030: build or adopt</seealso>
public partial class LineIconView
{
    /// <summary>Which drawing.</summary>
    [Parameter, EditorRequired]
    public DiagramIcon Icon { get; set; }

    /// <summary>The drawing's strokes, on the icons' 24 by 24 grid.</summary>
    private IReadOnlyList<string> Strokes => IconPaths.Of(Icon);
}
