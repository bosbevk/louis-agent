using Net.Codecrete.QrCodeGenerator;

namespace LouisAgent.Web.Paper;

/// <summary>
/// A QR code as data (FX154): how many cells a side the square is, its quiet edge included, and the path of its dark
/// cells. A link on paper can't be clicked, so a page draws its links this way when it is printed.
/// </summary>
/// <remarks>
/// The encoding is Net.Codecrete.QrCodeGenerator's (MIT, Louis's pick of three: small, no dependencies, and it
/// writes a path a page can draw). This record is the one place that names the package, so a component draws a
/// path and knows nothing of how it was made, and a change of encoder is a change here alone.
/// </remarks>
/// <param name="Size">The square's side in cells, the quiet edge on each side included.</param>
/// <param name="Path">The dark cells, as the data of an SVG path over that square.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0030-build-or-adopt.md">ADR 0030: build or adopt</seealso>
public sealed record QrDrawing(int Size, string Path)
{
    /// <summary>
    /// The quiet edge kept around a code, in cells, each side. The paper around it is white too, so two are enough
    /// for a phone to find where the code ends.
    /// </summary>
    public const int QuietEdge = 2;

    /// <summary>
    /// The code that holds this address, at the middle strength of error correction: it still reads with a crease
    /// or a smudge across it, without growing as large as the strongest would.
    /// </summary>
    public static QrDrawing Of(string address)
    {
        var code = QrCode.EncodeText(address, QrCode.Ecc.Medium);
        return new QrDrawing(code.Size + 2 * QuietEdge, code.ToGraphicsPath(QuietEdge));
    }
}
