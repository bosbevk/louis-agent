using System.Globalization;
using LouisAgent.Composition.Messages;
using LouisAgent.FilmExport.Messages;
using LouisAgent.FilmExport.Messages.Holes.Cards;

namespace LouisAgent.FilmExport.Cards.Pages;

/// <summary>
/// Says whether a card may be written: its type must have loaded, and its words must end inside the middle that a cut
/// to 3 to 2 keeps. When it may not, you get the reason, with the page it is for.
/// </summary>
/// <remarks>
/// Guard clauses, one case each (ADR 0051). The check is apart from the browser, so what decides whether a card is
/// right is tested without one, and the part that drives the browser stays thin.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
public static class CardFit
{
    /// <summary>
    /// Where a card's words may end, in points from the left: 1200 less the 128 every word starts at, so the words
    /// stand in the middle 944 that a cut to 3 to 2 keeps.
    /// </summary>
    public const double Middle = 1072;

    /// <summary>Why this page's card may not be written, or nothing when it may.</summary>
    public static string? Check(string page, double endsAt, bool typeLoaded)
    {
        if (!typeLoaded)
        {
            var missing = new CardTypeMissingHoles(page);
            return MessageText.Format(FilmExportUserText.CardTypeMissing, missing);
        }

        if (endsAt <= Middle) return null;

        var past = new CardRunsPastHoles(page, Math.Round(endsAt).ToString(CultureInfo.InvariantCulture),
            Middle.ToString(CultureInfo.InvariantCulture));
        return MessageText.Format(FilmExportUserText.CardRunsPast, past);
    }
}
