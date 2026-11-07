using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace LouisAgent.Packs.Web;

/// <summary>A PDF the web toolset fetched, as text, a page at a time.</summary>
/// <remarks>The files toolset reads PDFs the same way, and a pack references no other pack, so each keeps its own.</remarks>
internal static class PdfText
{
    /// <summary>Reads the whole PDF; throws when it can't be read.</summary>
    public static string Read(Stream stream)
    {
        // The library seeks; a network stream may not.
        using var seekable = new MemoryStream();
        stream.CopyTo(seekable);
        seekable.Position = 0;

        using var document = PdfDocument.Open(seekable);
        var text = new StringBuilder();
        foreach (var page in document.GetPages())
        {
            string content = ContentOrderTextExtractor.GetText(page).Trim();
            if (content.Length == 0) continue;
            text.AppendLine($"--- Page {page.Number} ---").AppendLine(content);
        }

        return text.ToString().Trim();
    }
}
