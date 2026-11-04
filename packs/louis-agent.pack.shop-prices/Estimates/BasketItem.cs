using System.Globalization;
using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Files;

namespace LouisAgent.Packs.ShopPrices.Estimates;

/// <summary>
/// One line of a shopping list as the model writes it ("beef mince 500 g", "eggs 18", "onions 1 kg x 2"): its name's
/// words, the pack's size when it gives one, and how many packs.
/// </summary>
/// <remarks>
/// A small reader over the line's tokens, no pattern (ADR 0057). A number before a weight or volume word is the pack's
/// size; a number next to "x" is how many packs; any other number counts items (eggs 18). Every other word is the
/// product's name.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: a regular expression is the last option</seealso>
public sealed record BasketItem(string Text, IReadOnlyList<string> Words, decimal? Amount, PackUnit Unit, int Packs)
{
    /// <summary>The weight and volume words, and what one of each is in grams or millilitres.</summary>
    private static readonly IReadOnlyDictionary<string, (decimal Factor, PackUnit Unit)> Units =
        new Dictionary<string, (decimal, PackUnit)>(StringComparer.OrdinalIgnoreCase)
        {
            ["g"] = (1, PackUnit.Gram),
            ["gr"] = (1, PackUnit.Gram),
            ["gram"] = (1, PackUnit.Gram),
            ["grams"] = (1, PackUnit.Gram),
            ["kg"] = (1000, PackUnit.Gram),
            ["kilo"] = (1000, PackUnit.Gram),
            ["kilogram"] = (1000, PackUnit.Gram),
            ["kilograms"] = (1000, PackUnit.Gram),
            ["ml"] = (1, PackUnit.Millilitre),
            ["millilitre"] = (1, PackUnit.Millilitre),
            ["milliliter"] = (1, PackUnit.Millilitre),
            ["l"] = (1000, PackUnit.Millilitre),
            ["litre"] = (1000, PackUnit.Millilitre),
            ["litres"] = (1000, PackUnit.Millilitre),
            ["liter"] = (1000, PackUnit.Millilitre),
            ["liters"] = (1000, PackUnit.Millilitre),
        };

    /// <summary>Reads a line; a line with no name words is still a line, and simply matches nothing.</summary>
    public static BasketItem Parse(string line)
    {
        var tokens = Tokens(line);
        var name = new List<string>();
        decimal? amount = null;
        var unit = PackUnit.Each;
        var packs = 1;
        for (var i = 0; i < tokens.Count; i++)
        {
            string token = tokens[i];
            bool timesBefore = i > 0 && IsTimes(tokens[i - 1]);
            // "2 x onions" counts packs before the x; in "eggs 6 x 2" the number after it does.
            bool timesAfter = i + 1 < tokens.Count && IsTimes(tokens[i + 1]) && !IsNumber(tokens, i + 2);
            // An "x" is read with the number beside it.
            if (IsTimes(token) && (IsNumber(tokens, i - 1) || IsNumber(tokens, i + 1))) continue;
            if (!decimal.TryParse(token, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number))
            {
                name.Add(token);
                continue;
            }

            if (timesBefore || timesAfter)
            {
                packs = Math.Max(1, (int)number);
                continue;
            }

            if (i + 1 < tokens.Count && Units.TryGetValue(tokens[i + 1], out var size))
            {
                (amount, unit) = (number * size.Factor, size.Unit);
                i++;
                continue;
            }

            (amount, unit) = (number, PackUnit.Each);
        }

        return new BasketItem(line.Trim(), ProductWords.Of(string.Join(' ', name)), amount, unit, packs);
    }

    /// <summary>Whether a token is the "times" of a pack count: x, X or ×.</summary>
    private static bool IsTimes(string token) => token is "x" or "X" or "×";

    /// <summary>Whether the token at this place is a number.</summary>
    private static bool IsNumber(IReadOnlyList<string> tokens, int at) =>
        at >= 0 && at < tokens.Count &&
        decimal.TryParse(tokens[at], NumberStyles.Number, CultureInfo.InvariantCulture, out _);

    /// <summary>
    /// The line's tokens: split on spaces, with a number and the word stuck to it pulled apart ("500g", "2x") and a
    /// decimal comma read as a point ("2,5 kg").
    /// </summary>
    private static List<string> Tokens(string line)
    {
        var spaced = new System.Text.StringBuilder();
        string text = line ?? "";
        for (var i = 0; i < text.Length; i++)
        {
            char c = text[i];
            bool commaInNumber = c == ',' && i > 0 && i + 1 < text.Length && char.IsDigit(text[i - 1]) &&
                                 char.IsDigit(text[i + 1]);
            if (commaInNumber)
            {
                spaced.Append('.');
                continue;
            }

            // A switch between a digit and a letter starts a new token.
            if (i > 0 && (char.IsDigit(c) && char.IsLetter(text[i - 1]) || char.IsLetter(c) && char.IsDigit(text[i - 1])))
                spaced.Append(' ');
            spaced.Append(c is '(' or ')' or ',' or ';' ? ' ' : c);
        }

        return [.. spaced.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
    }
}
