using LouisAgent.Composition.Sites;

namespace LouisAgent.Api.Site.Places;

/// <summary>
/// Finds the places that hold a visitor's words (F127.7): a word in a place's heading counts three times one in its
/// text, and of places that score the same, the smallest (a piece before the section around it) comes first, then the
/// one met first. Plain word matching, case ignored: the simplest search that finds a tile by its words (ADR 0055).
/// </summary>
internal static class PlaceSearch
{
    /// <summary>
    /// The best places for the words, at most <paramref name="most"/>; none when no word is two letters.
    /// </summary>
    public static IReadOnlyList<SitePlace> Find(IReadOnlyList<SitePlace> places, string words, int most)
    {
        var asked = words.Split((char[])[' ', ',', '.', '?', '!'], StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.Length > 1).ToList();
        if (asked.Count == 0) return [];

        return places.Select((place, at) => (Place: place, At: at, Score: ScoreOf(place, asked)))
            .Where(scored => scored.Score > 0)
            .OrderByDescending(scored => scored.Score)
            .ThenByDescending(scored => scored.Place.Kind)
            .ThenBy(scored => scored.At)
            .Take(most).Select(scored => scored.Place).ToList();
    }

    /// <summary>How well a place holds the words: three for each in its heading, one for each in its text.</summary>
    private static int ScoreOf(SitePlace place, List<string> asked) => asked.Sum(word =>
        (Holds(place.Heading, word) ? 3 : 0) + (Holds(place.Text, word) ? 1 : 0));

    /// <summary>Whether the text holds the word, case ignored.</summary>
    private static bool Holds(string text, string word) => text.Contains(word, StringComparison.OrdinalIgnoreCase);
}
