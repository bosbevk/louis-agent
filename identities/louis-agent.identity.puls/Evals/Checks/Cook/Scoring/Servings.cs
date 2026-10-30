using System.Globalization;
using System.Text.RegularExpressions;

namespace LouisAgent.Identities.Puls.Evals.Checks.Cook.Scoring;

/// <summary>
/// Reads how many people an answer says it serves, in Afrikaans or English, in digits or words: `vir 4 mense`,
/// `vir vier mense`, `4 people`, `for 4`, `serves 4`, `feeds six`. It reads what an answer names, never what it should.
/// </summary>
/// <remarks>
/// A time (`for 45 minutes`), a weight or a volume after the number isn't a count of people, so a unit after it rules
/// it out; a price isn't read at all.
/// </remarks>
public static partial class Servings
{
    /// <summary>A number from one to twelve, in digits or in Afrikaans or English words.</summary>
    private const string Number =
        @"(?<n>\d{1,2}|een|twee|drie|vier|vyf|ses|sewe|agt|nege|tien|elf|twaalf|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve)";

    /// <summary>
    /// A count after a word that introduces who it's for, unless a unit follows it (a time, a weight, a volume).
    /// Matches "vir vier"; not "for 45 min".
    /// </summary>
    [GeneratedRegex(
        @"\b(?:vir|for|serves|serving|feeds|bedien|voed)\s*:?\s*" + Number +
        @"\b(?!\s*(?:min|uur|ure|hour|hr|sek|sec|dae|day|g\b|kg|ml|l\b|liter|cup|koppie|eetlepel|teelepel|tbsp|tsp|%|°|grade|degrees))",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Introduced();

    /// <summary>A count in front of the word for people. Matches "4 mense"; not "4 eiers".</summary>
    [GeneratedRegex(
        @"\b" + Number +
        @"\s+(?:mense|persone|persoon|mensies|porsies|porsie|gaste|people|persons|guests|servings|portions|adults|diners)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Counted();

    /// <summary>The numbers written as words, Afrikaans and English, case ignored.</summary>
    private static readonly IReadOnlyDictionary<string, int> Words =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["een"] = 1,
            ["twee"] = 2,
            ["drie"] = 3,
            ["vier"] = 4,
            ["vyf"] = 5,
            ["ses"] = 6,
            ["sewe"] = 7,
            ["agt"] = 8,
            ["nege"] = 9,
            ["tien"] = 10,
            ["elf"] = 11,
            ["twaalf"] = 12,
            ["one"] = 1,
            ["two"] = 2,
            ["three"] = 3,
            ["four"] = 4,
            ["five"] = 5,
            ["six"] = 6,
            ["seven"] = 7,
            ["eight"] = 8,
            ["nine"] = 9,
            ["ten"] = 10,
            ["eleven"] = 11,
            ["twelve"] = 12,
        };

    /// <summary>Every number of people the answer names, in the order it first says them, each once.</summary>
    public static IReadOnlyList<int> Of(string? answer)
    {
        string text = answer ?? "";
        return Introduced().Matches(text).Concat(Counted().Matches(text))
            .OrderBy(match => match.Index)
            .Select(match => match.Groups["n"].Value)
            .Select(word => int.TryParse(word, NumberStyles.None, CultureInfo.InvariantCulture, out int n)
                ? n
                : Words[word])
            .Where(n => n > 0)
            .Distinct()
            .ToList();
    }
}
