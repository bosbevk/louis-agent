using System.Globalization;

namespace louis_agent.web.Services;

/// <summary>
/// The usage line under an answer and the chat's total (F3), e.g. <c>1.9k in · 26k cached · 412 out · $0.004</c>, or
/// <c>R0.066</c> in rands (F14). The same number rules as core's <c>UsageText</c> (the web app doesn't reference core):
/// k/M token counts, two significant digits below 1, "price unknown" rather than 0 when nothing was priced, and always
/// <c>.</c> as the decimal mark.
/// </summary>
public static class UsageFormat
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>The short line: tokens by kind, then the cost. Kinds with no tokens are left out, except in and out.</summary>
    public static string Line(TurnUsage usage, CurrencyChoice? currency = null)
    {
        var parts = new List<string> { $"{Tokens(usage.InputTokens)} in" };
        if (usage.CacheCreationInputTokens > 0) parts.Add($"{Tokens(usage.CacheCreationInputTokens)} cache write");
        if (usage.CacheReadInputTokens > 0) parts.Add($"{Tokens(usage.CacheReadInputTokens)} cached");
        parts.Add($"{Tokens(usage.OutputTokens)} out");
        parts.Add(Cost(usage, currency));
        return string.Join(" · ", parts);
    }

    /// <summary>The exact figures, for the line's tooltip: what was charged, and the rate when converted.</summary>
    public static string Detail(TurnUsage usage, CurrencyChoice? currency = null) =>
        $"{Requests(usage.Requests)}: {usage.InputTokens:N0} input, {usage.CacheCreationInputTokens:N0} cache write, " +
        $"{usage.CacheReadInputTokens:N0} cache read, {usage.OutputTokens:N0} output tokens" +
        (usage.Cost is { } cost ? $"; {cost.ToString("0.######", Invariant)} {usage.Currency} charged" : "; price unknown") +
        (usage.Unpriced > 0 && usage.Cost is not null ? $" ({usage.Unpriced} unpriced)" : "") +
        (usage.Cost is not null && Converts(usage.Currency, currency)
            ? $"; at {currency!.Rate.ToString("0.####", Invariant)} {currency.Code}/{currency.Base} on {currency.AsOf}"
            : "");

    /// <summary>The chat's total, e.g. <c>This chat: 5 requests · 84k tokens · R0.35</c>.</summary>
    public static string Total(TurnUsage usage, CurrencyChoice? currency = null) =>
        $"This chat: {Requests(usage.Requests)} · {Tokens(usage.TotalTokens)} tokens · {Cost(usage, currency)}";

    /// <summary>The cost; a partly priced total says how many requests have no price, so it isn't read as the whole.</summary>
    public static string Cost(TurnUsage usage, CurrencyChoice? currency = null) => usage.Cost switch
    {
        null => "price unknown",
        { } amount when usage.Unpriced > 0 => $"{Money(amount, usage.Currency, currency)} + {usage.Unpriced} unpriced",
        { } amount => Money(amount, usage.Currency, currency),
    };

    /// <summary>
    /// The amount, converted to <paramref name="choice"/> when it was charged in its base; written with the symbol in
    /// front (R0.11, $0.006) or, for a currency without one, the code after the number (0.04 EUR).
    /// </summary>
    public static string Money(decimal amount, string? currency, CurrencyChoice? choice = null)
    {
        string code = currency ?? "USD";
        string symbol = code == "USD" ? "$" : code;
        if (Converts(currency, choice))
        {
            amount *= choice!.Rate;
            code = choice.Code;
            symbol = choice.Symbol;
        }

        string number = amount == 0 ? "0"
            : Math.Abs(amount) >= 1 ? amount.ToString("0.00", Invariant)
            : SignificantDigits(amount, 2);
        return symbol == code ? $"{number} {code}" : $"{symbol}{number}";
    }

    public static string Tokens(long count) => count switch
    {
        < 1_000 => count.ToString(Invariant),
        < 999_500 => Short(count / 1_000m) + "k",
        _ => Short(count / 1_000_000m) + "M",
    };

    private static bool Converts(string? currency, CurrencyChoice? choice) =>
        choice is { Converts: true } && (currency ?? "USD") == choice.Base;

    private static string Short(decimal value) =>
        value < 10 ? Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.#", Invariant)
            : Math.Round(value, 0, MidpointRounding.AwayFromZero).ToString("0", Invariant);

    private static string SignificantDigits(decimal amount, int digits)
    {
        int decimals = Math.Clamp(digits - 1 - (int)Math.Floor(Math.Log10((double)Math.Abs(amount))), 0, 6);
        return Math.Round(amount, decimals, MidpointRounding.AwayFromZero).ToString("0.######", Invariant);
    }

    private static string Requests(int count) => count == 1 ? "1 request" : $"{count} requests";
}