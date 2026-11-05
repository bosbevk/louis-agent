using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace LouisAgent.Packs.Web.Tests;

/// <summary>A small real PDF for the tests that fetch one.</summary>
internal static class PdfFixture
{
    /// <summary>A one-page PDF holding these lines.</summary>
    public static byte[] Of(params string[] lines)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        var page = builder.AddPage(PageSize.A4);
        for (int i = 0; i < lines.Length; i++) page.AddText(lines[i], 12, new PdfPoint(40, 780 - i * 20), font);
        return builder.Build();
    }
}
