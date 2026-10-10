using System.Globalization;

namespace louis_agent.web.Services;

/// <summary>
/// The usage line under an answer and the chat's total (F3), e.g. <c>1.9k in · 26k cached · 412 out · $0.004</c>.
/// The same number rules as core's <c>UsageText</c> (the web app doesn't reference core): k/M token counts, two
/// significant digits below $1, "price unknown" rather than $0 when nothing was priced.
/// </summary>
public static class UsageFormat
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>The short line: tokens by kind, then the cost. Kinds with no tokens are left out, except in and out.</summary>
    public static string Line(TurnUsage usage)
    {
        var parts = new List<string> { $"{Tokens(usage.InputTokens)} in" };
        if (usage.CacheCreationInputTokens > 0) parts.Add($"{Tokens(usage.CacheCreationInputTokens)} cache write");
        if (usage.CacheReadInputTokens > 0) parts.Add($"{Tokens(usage.CacheReadInputTokens)} cached");
        parts.Add($"{Tokens(usage.OutputTokens)} out");
        parts.Add(Cost(usage));
        return string.Join(" · ", parts);
    }

    /// <summary>The exact figures, for the line's tooltip.</summary>
    public static string Detail(TurnUsage usage) =>
        $"{Requests(usage.Requests)}: {usage.InputTokens:N0} input, {usage.CacheCreationInputTokens:N0} cache write, " +
        $"{usage.CacheReadInputTokens:N0} cache read, {usage.OutputTokens:N0} output tokens" +
        (usage.Cost is { } cost ? $"; {cost.ToString("0.######", Invariant)} {usage.Currency}" : "; price unknown") +
        (usage.Unpriced > 0 && usage.Cost is not null ? $" ({usage.Unpriced} unpriced)" : "");

    /// <summary>The chat's total, e.g. <c>This chat: 5 requests · 84k tokens · $0.021</c>.</summary>
    public static string Total(TurnUsage usage) =>
        $"This chat: {Requests(usage.Requests)} · {Tokens(usage.TotalTokens)} tokens · {Cost(usage)}";

    /// <summary>The cost; a partly priced total says how many requests have no price, so it isn't read as the whole.</summary>
    public static string Cost(TurnUsage usage) => usage.Cost switch
    {
        null => "price unknown",
        { } amount when usage.Unpriced > 0 => $"{Money(amount, usage.Currency)} + {usage.Unpriced} unpriced",
        { } amount => Money(amount, usage.Currency),
    };

    public static string Money(decimal amount, string? currency)
    {
        string number = amount == 0 ? "0"
            : Math.Abs(amount) >= 1 ? amount.ToString("0.00", Invariant)
            : SignificantDigits(amount, 2);
        return currency is null or "USD" ? $"${number}" : $"{number} {currency}";
    }

    public static string Tokens(long count) => count switch
    {
        < 1_000 => count.ToString(Invariant),
        < 999_500 => Short(count / 1_000m) + "k",
        _ => Short(count / 1_000_000m) + "M",
    };

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