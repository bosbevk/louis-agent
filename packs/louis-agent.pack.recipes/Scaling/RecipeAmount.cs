using System.Globalization;

namespace LouisAgent.Packs.Recipes.Scaling;

/// <summary>
/// Reads the amount a recipe line starts with, the way sites write it (<c>1 ½ cups</c>, <c>1/2 tsp</c>, <c>1,5 kg</c>,
/// <c>2-3</c>), multiplies it, and writes it back as a cook would measure it. Everything else on the line stays as the
/// page has it, and a line with no number at its start isn't touched.
/// </summary>
/// <remarks>
/// A small reader over plain string methods, not a regular expression (ADR 0057). It only does the sums: which factor
/// fits, and what an amount in a step means, stay the cook's (ADR 0080).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
internal static class RecipeAmount
{
    private static readonly HashSet<string> Weights = new(
        "g gram grams kg kilogram kilograms ml millilitre millilitres l litre litres liter liters".Split(' '),
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> Measures = new(
        ("cup cups koppie koppies tbsp tbs tablespoon tablespoons eetlepel eetlepels tsp teaspoon teaspoons teelepel " +
         "teelepels tot tots k t e").Split(' '),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>What can't be split in a kitchen, so it comes out whole.</summary>
    private static readonly HashSet<string> CantSplit = new(
        "egg eggs eier eiers yolk yolks white whites roll rolls rolletjie rolletjies".Split(' '),
        StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> PeopleWords = new(
        "serving servings people person persons porsie porsies mense portion portions".Split(' '),
        StringComparer.OrdinalIgnoreCase);

    private static readonly string[] RangeMarks = [" - ", " – ", " to ", "-", "–"];

    /// <summary>Words that only hedge the amount after them.</summary>
    private static readonly string[] Hedges = ["about ", "approx. ", "approx ", "approximately ", "roughly ", "circa ", "ongeveer "];

    /// <summary>A number written out, which this doesn't scale but mustn't pass over in silence.</summary>
    private static readonly HashSet<string> NumberWords = new(
        "one two three four five six seven eight nine ten half een twee drie vier vyf ses sewe agt nege tien".Split(' '),
        StringComparer.OrdinalIgnoreCase);

    private static readonly (char Mark, double Value)[] Fractions =
    [
        ('⅛', 0.125), ('¼', 0.25), ('⅓', 1.0 / 3), ('½', 0.5), ('⅔', 2.0 / 3), ('¾', 0.75)
    ];

    /// <summary>How a kind of amount is written back.</summary>
    private enum Kind
    {
        /// <summary>A plain number: <c>7.5 ml</c>.</summary>
        Plain,

        /// <summary>A fraction a cook can measure: <c>1⅓ cups</c>.</summary>
        Measured,

        /// <summary>A whole one, at least one: <c>3 eggs</c>.</summary>
        Whole
    }

    /// <summary>A number's value and how many characters it took.</summary>
    private readonly record struct Number(double Value, int Length);

    /// <summary>An amount, its second end if it's a range, how it's joined, and how many characters it took.</summary>
    private readonly record struct Amount(double Low, double? High, string Mark, int Length);

    /// <summary>The line with its amount multiplied, and a second measure in brackets straight after it too.</summary>
    public static string Scale(string line, double factor)
    {
        if (HedgeOn(line) is { } hedge) return line[..hedge.Length] + Scale(line[hedge.Length..], factor);
        if (factor == 1 || ReadAmount(line, 0) is not { } amount) return line;

        string rest = line[amount.Length..];
        string written = Write(amount, factor, KindOf(rest));
        return SaidTwiceAt(rest) is { } bracket
            ? written + rest[..(bracket + 1)] + Scale(rest[(bracket + 1)..], factor)
            : written + rest;
    }

    /// <summary>The word hedging this line's amount, if one does.</summary>
    private static string? HedgeOn(string line) =>
        Hedges.FirstOrDefault(word => line.StartsWith(word, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Where a bracket says the same amount again: "125 ml (1/2 cup)" is one amount said twice, where "1 packet (10g)"
    /// is a pack's size, which stays.
    /// </summary>
    private static int? SaidTwiceAt(string rest)
    {
        int bracket = rest.IndexOf('(');
        return bracket > 0 && IsAUnit(rest[..bracket].Trim()) ? bracket : null;
    }

    /// <summary>Whether a line holds an amount at all: a digit, a fraction or a number in words.</summary>
    public static bool HoldsAnAmount(string line) =>
        line.Any(c => char.IsAsciiDigit(c) || FractionOf(c) is not null) || Words(line).Any(NumberWords.Contains);

    /// <summary>
    /// Whether a number is left on the line after the amount <see cref="Scale"/> scales. It can't tell a pack's size
    /// from a second ingredient run into the line, so it says so and the cook decides, since a wrong number that looks
    /// right is worse than a marked line.
    /// </summary>
    public static bool LeavesAnAmount(string line)
    {
        if (HedgeOn(line) is { } hedge) return LeavesAnAmount(line[hedge.Length..]);
        if (ReadAmount(line, 0) is not { } amount) return false;

        string rest = line[amount.Length..];
        return SaidTwiceAt(rest) is { } bracket
            ? LeavesAnAmount(rest[(bracket + 1)..])
            : rest.Any(c => char.IsAsciiDigit(c) || FractionOf(c) is not null);
    }

    /// <summary>How many people a yield is for: a range's middle, and nothing when it counts something else.</summary>
    public static double? PeopleIn(string? yield)
    {
        string words = (yield ?? "").Trim();
        if (ReadAmount(words, 0) is not { } amount) return null;

        string rest = words[amount.Length..].Trim();
        if (rest.Length > 0 && !PeopleWords.Contains(FirstWord(rest))) return null;

        return amount.High is { } high ? (amount.Low + high) / 2 : amount.Low;
    }

    /// <summary>A step's amounts of food and what they become. Times, temperatures and sizes have no unit.</summary>
    public static IReadOnlyList<(string From, string To)> InStep(string step, double factor)
    {
        var found = new List<(string, string)>();
        if (factor == 1) return found;

        for (int at = 0; at < step.Length; at++)
        {
            if (at > 0 && char.IsLetterOrDigit(step[at - 1])) continue;
            if (ReadAmount(step, at) is not { } amount) continue;

            string unit = FirstWord(step[(at + amount.Length)..]);
            int end = step.IndexOf(unit, at + amount.Length, StringComparison.Ordinal) + unit.Length;

            // "2-litre" is the size of a pot, and "1 t" could be anything in a sentence.
            bool joined = at + amount.Length < step.Length && step[at + amount.Length] == '-';
            if (!joined && unit.Length > 1 && IsAUnit(unit))
                found.Add((step[at..end], Write(amount, factor, KindOf(unit)) + step[(at + amount.Length)..end]));

            at += amount.Length - 1;
        }

        return found;
    }

    /// <summary>Whether this word is a weight, a volume or a kitchen measure.</summary>
    private static bool IsAUnit(string word) => Weights.Contains(word) || Measures.Contains(word);

    /// <summary>How an amount followed by these words is written: by its unit, or by what it counts.</summary>
    /// <remarks>Guard clauses, one per kind (ADR 0051).</remarks>
    private static Kind KindOf(string rest)
    {
        string[] words = Words(rest);
        if (words.Length > 0 && Weights.Contains(words[0])) return Kind.Plain;
        if (words.Length > 0 && Measures.Contains(words[0])) return Kind.Measured;

        return words.Take(3).Any(CantSplit.Contains) ? Kind.Whole : Kind.Measured;
    }

    /// <summary>The amount multiplied, with its range mark as the page wrote it.</summary>
    private static string Write(Amount amount, double factor, Kind kind)
    {
        string low = Write(amount.Low * factor, kind);
        return amount.High is { } high ? low + amount.Mark + Write(high * factor, kind) : low;
    }

    /// <summary>One number as a cook would measure it.</summary>
    private static string Write(double value, Kind kind) => kind switch
    {
        Kind.Whole => Math.Max(1, Math.Round(value, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture),
        Kind.Plain when value >= 10 => Math.Round(value, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture),
        Kind.Plain => value.ToString("0.##", CultureInfo.InvariantCulture),
        _ => AsFraction(value)
    };

    /// <summary>A number with the nearest fraction a cook has a spoon or a cup for.</summary>
    private static string AsFraction(double value)
    {
        double whole = Math.Floor(value);
        double part = value - whole;
        var nearest = Fractions.MinBy(fraction => Math.Abs(fraction.Value - part));

        // Closer to no fraction at all, or to the next whole one, than to any of them.
        if (part < Math.Abs(nearest.Value - part) && whole > 0) return whole.ToString(CultureInfo.InvariantCulture);
        if (1 - part < Math.Abs(nearest.Value - part)) return (whole + 1).ToString(CultureInfo.InvariantCulture);

        return (whole > 0 ? whole.ToString(CultureInfo.InvariantCulture) : "") + nearest.Mark;
    }

    /// <summary>The number at this place, with a second one if it's a range.</summary>
    private static Amount? ReadAmount(string text, int at)
    {
        if (ReadNumber(text, at) is not { } first) return null;

        int after = at + first.Length;
        foreach (string mark in RangeMarks)
        {
            if (string.CompareOrdinal(text, after, mark, 0, mark.Length) != 0) continue;
            if (ReadNumber(text, after + mark.Length) is { } second)
                return new Amount(first.Value, second.Value, mark, first.Length + mark.Length + second.Length);
        }

        return new Amount(first.Value, null, "", first.Length);
    }

    /// <summary>
    /// A number as sites write it: <c>2</c>, <c>1.5</c>, <c>1,5</c>, <c>½</c>, <c>1/2</c>, <c>1½</c>, <c>1 ½</c>,
    /// <c>1 1/2</c>.
    /// </summary>
    private static Number? ReadNumber(string text, int at)
    {
        if (at >= text.Length) return null;
        if (FractionOf(text[at]) is { } alone) return new Number(alone, 1);
        if (ReadDigits(text, at) is not { } whole) return null;

        int next = at + whole.Length;
        if (ReadSlash(text, at) is { } slash) return slash;
        if (next + 1 < text.Length && text[next] is '.' or ',' && ReadDigits(text, next + 1) is { } part)
            return new Number(double.Parse($"{whole.Value}.{text.Substring(next + 1, part.Length)}", CultureInfo.InvariantCulture),
                whole.Length + 1 + part.Length);

        return ReadTheFractionAfter(text, next) is { } extra
            ? new Number(whole.Value + extra.Value, whole.Length + extra.Length)
            : whole;
    }

    /// <summary>The fraction straight after a whole number, if any, with what it took.</summary>
    private static Number? ReadTheFractionAfter(string text, int at)
    {
        if (at >= text.Length) return null;
        if (FractionOf(text[at]) is { } touching) return new Number(touching, 1);
        if (text[at] != ' ' || at + 1 >= text.Length) return null;
        if (FractionOf(text[at + 1]) is { } spaced) return new Number(spaced, 2);

        return ReadSlash(text, at + 1) is { } slash ? new Number(slash.Value, slash.Length + 1) : null;
    }

    /// <summary>A written fraction such as <c>1/2</c> at this place.</summary>
    private static Number? ReadSlash(string text, int at)
    {
        if (ReadDigits(text, at) is not { } top) return null;

        int slash = at + top.Length;
        if (slash >= text.Length || text[slash] != '/') return null;

        return ReadDigits(text, slash + 1) is { Value: > 0 } bottom
            ? new Number(top.Value / bottom.Value, top.Length + 1 + bottom.Length)
            : null;
    }

    /// <summary>A run of digits at this place.</summary>
    private static Number? ReadDigits(string text, int at)
    {
        int length = text.Skip(at).TakeWhile(char.IsAsciiDigit).Count();
        return length == 0 ? null : new Number(double.Parse(text.Substring(at, length), CultureInfo.InvariantCulture), length);
    }

    /// <summary>The value of a fraction written as one character.</summary>
    private static double? FractionOf(char mark) =>
        Fractions.Where(fraction => fraction.Mark == mark).Select(fraction => (double?)fraction.Value).FirstOrDefault();

    /// <summary>The first word, or nothing.</summary>
    private static string FirstWord(string text) => Words(text).FirstOrDefault() ?? "";

    /// <summary>The runs of letters in a text.</summary>
    private static string[] Words(string text) =>
        new string([.. text.Select(c => char.IsLetter(c) ? c : ' ')]).Split(' ', StringSplitOptions.RemoveEmptyEntries);
}
