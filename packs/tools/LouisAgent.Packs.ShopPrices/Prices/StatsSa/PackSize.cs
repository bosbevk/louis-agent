using System.Globalization;
using LouisAgent.Packs.ShopPrices.Prices.Files;

namespace LouisAgent.Packs.ShopPrices.Prices.StatsSa;

/// <summary>
/// A pack size Stats SA writes in words ("1,5 Kilogram", "10 X 18 Gram", "Box Of 12") as an amount we can scale by:
/// grams, millilitres or items. A size that is none of those ("32 Inch", "Pack Of 3 Pairs") is <see cref="PackUnit.Other"/>
/// with no amount, priced only as it is, never guessed.
/// </summary>
/// <remarks>
/// An Interpreter for a tiny language (Gang of Four), written as plain checks on the words (ADR 0057): a size is one
/// of a few shapes, each tried in turn. Every shape in the releases from January 2017 to August 2026 is in the tests.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
public sealed record PackSize(decimal? Amount, PackUnit Unit)
{
    /// <summary>What we can't scale by.</summary>
    public static readonly PackSize Other = new(null, PackUnit.Other);

    /// <summary>The words for a weight or a volume, and how many grams or millilitres one is.</summary>
    private static readonly IReadOnlyDictionary<string, (decimal Factor, PackUnit Unit)> Measures =
        new Dictionary<string, (decimal, PackUnit)>(StringComparer.OrdinalIgnoreCase)
        {
            ["gram"] = (1m, PackUnit.Gram),
            ["kilogram"] = (1000m, PackUnit.Gram),
            ["millilitre"] = (1m, PackUnit.Millilitre),
            ["litre"] = (1000m, PackUnit.Millilitre),
        };

    /// <summary>The words that open a count ("Box Of 12", "Set Of 5").</summary>
    private static readonly HashSet<string> Counts = new(["box", "pack", "set"], StringComparer.OrdinalIgnoreCase);

    /// <summary>The words that mean one item ("Each", "Per Bunch", "Per Pack").</summary>
    private static readonly HashSet<string> Ones = new(["each", "bunch", "pack"], StringComparer.OrdinalIgnoreCase);

    /// <summary>The size those words mean, or <see cref="Other"/>.</summary>
    public static PackSize Read(string size)
    {
        string[] words = size.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return Measure(words) ?? Multiple(words) ?? Count(words) ?? One(words) ?? Other;
    }

    /// <summary>"1,5 Kilogram", or "Per Kilogram" for one.</summary>
    private static PackSize? Measure(string[] words)
    {
        if (words.Length != 2 || !Measures.TryGetValue(words[1], out var measure)) return null;
        decimal? number = Is(words[0], "per") ? 1m : Number(words[0]);
        return number is { } n ? new PackSize(PlainNumber.Of(n * measure.Factor), measure.Unit) : null;
    }

    /// <summary>"10 X 18 Gram": so many packs of a weight or a volume, as one.</summary>
    private static PackSize? Multiple(string[] words)
    {
        if (words.Length != 4 || !Is(words[1], "x") || Number(words[0]) is not { } count) return null;
        return Measure([words[2], words[3]]) is { Amount: { } amount } each ? each with { Amount = PlainNumber.Of(count * amount) } : null;
    }

    /// <summary>"Box Of 12", "For 20", "1 Unit", "Half cabbage": a number of items.</summary>
    private static PackSize? Count(string[] words)
    {
        decimal? count = words switch
        {
            [var box, var of, var n] when Counts.Contains(box) && Is(of, "of") => Number(n),
            [var forWord, var n] when Is(forWord, "for") => Number(n),
            [var n, var unit] when Is(unit, "unit") => Number(n),
            [var half, _] when Is(half, "half") => 0.5m,
            _ => null,
        };
        return count is { } c ? new PackSize(c, PackUnit.Each) : null;
    }

    /// <summary>"Each", "Per Bunch", "Per Pack": one item.</summary>
    private static PackSize? One(string[] words) => words switch
    {
        [var each] when Is(each, "each") => new PackSize(1m, PackUnit.Each),
        [var per, var what] when Is(per, "per") && Ones.Contains(what) => new PackSize(1m, PackUnit.Each),
        _ => null,
    };

    /// <summary>A number written with a comma or a point ("1,5", "1.75").</summary>
    private static decimal? Number(string text) =>
        decimal.TryParse(text.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
            out decimal number)
            ? PlainNumber.Of(number)
            : null;

    /// <summary>The word is that word, case ignored.</summary>
    private static bool Is(string word, string expected) => string.Equals(word, expected, StringComparison.OrdinalIgnoreCase);
}
