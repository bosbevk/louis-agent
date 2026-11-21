using LouisAgent.Web.Paper;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Paper;

/// <summary>
/// A QR code drawn as a picture (FX154): black cells on a white square, sharp at any size since it is drawn, not
/// a bitmap. It draws what it is given to hold and knows nothing of links or of paper.
/// </summary>
public partial class QrImage
{
    private QrDrawing _drawing = new(0, "");

    /// <summary>What the code holds: an address, most often.</summary>
    [Parameter, EditorRequired]
    public string Value { get; set; } = "";

    /// <summary>What the picture is, for someone who can't see it.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>The square's corners, for the picture's own measure.</summary>
    private string Box => $"0 0 {_drawing.Size} {_drawing.Size}";

    /// <summary>Draws the code again only when what it holds has changed.</summary>
    protected override void OnParametersSet() => _drawing = QrDrawing.Of(Value);
}
