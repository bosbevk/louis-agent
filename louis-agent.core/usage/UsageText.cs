namespace louis_agent.core.usage;

using System.Globalization;

/// <summary>
/// The usage text every host shows (F3): the line after an answer and the <c>/usage</c> reply. One format in core, so the
/// CLI, the API and Rider say the same thing; totals come from <see cref="UsageReport.Total"/>, like a ledger report.
/// </summary>
public static class UsageText
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>
    /// The line after an answer, e.g. <c>turn: 3 requests · 31k tokens (26k cached) · $0.006 · session $0.04</c>.
    /// </summary>
    /// <param name="turn">The turn's records (<see cref="UsageScope.Records"/>).</param>
    /// <param name="session">The session's records so far, this turn included; null to leave the session total out.</param>
    public static string Line(IReadOnlyList<UsageRecord> turn, IReadOnlyList<UsageRecord>? session = null)
    {
        string line = "turn: " + Describe(UsageReport.Total("turn", turn), CurrencyOf(turn));
        if (session is null) return line;

        UsageTotal total = UsageReport.Total("session", session);
        return $"{line} · session {Cost(total, CurrencyOf(session))}";
    }

    /// <summary>The <c>/usage</c> reply: the session's total, one line per purpose, and the tokens by kind.</summary>
    public static string Summary(IReadOnlyList<UsageRecord> session)
    {
        if (session.Count == 0) return "No model requests in this session yet.";

        string currency = CurrencyOf(session);
        UsageTotal total = UsageReport.Total("session", session);
        // A markdown list: the web app and Rider render replies as markdown, where single line breaks would run the lines
        // into one paragraph; a terminal shows the list as it is.
        var lines = new List<string> { $"Session usage: {Describe(total, currency)}" };
        lines.AddRange(UsageReport.Totals(session, r => r.Purpose).Select(p => $"- {p.Key}: {Describe(p, currency)}"));
        lines.Add($"- tokens: {Tokens(total.Input)} input · {Tokens(total.CacheWrite)} cache write · " +
                  $"{Tokens(total.CacheRead)} cache read · {Tokens(total.Output)} output");
        if (total.Unpriced > 0)
            lines.Add($"- price unknown for {Requests(total.Unpriced)}: the model isn't in the price table");
        // "\n", not Environment.NewLine: the same text on every OS and over the API.
        return string.Join("\n", lines);
    }

    /// <summary><c>3 requests · 31k tokens (26k cached) · $0.006</c>.</summary>
    internal static string Describe(UsageTotal total, string currency)
    {
        long tokens = total.Input + total.CacheWrite + total.CacheRead + total.Output;
        string cached = total.CacheRead > 0 ? $" ({Tokens(total.CacheRead)} cached)" : "";
        return $"{Requests(total.Requests)} · {Tokens(tokens)} tokens{cached} · {Cost(total, currency)}";
    }

    /// <summary>
    /// The cost, or "price unknown" when no record has one. A total with some unpriced records says how many, so a partial
    /// sum is never read as the whole cost.
    /// </summary>
    internal static string Cost(UsageTotal total, string currency) => total.Cost switch
    {
        null => "price unknown",
        { } amount when total.Unpriced > 0 => $"{Money(amount, currency)} + {total.Unpriced} unpriced",
        { } amount => Money(amount, currency),
    };

    /// <summary>
    /// Money for reading, not accounting (the ledger keeps 6 decimals): cents from $1 up, else two significant digits, so a
    /// cheap turn still shows a number: $0.006, $0.037, $0.66, $12.35. Other currencies as "0.04 EUR".
    /// </summary>
    internal static string Money(decimal amount, string currency)
    {
        string number = amount == 0 ? "0"
            : Math.Abs(amount) >= 1 ? amount.ToString("0.00", Invariant)
            : SignificantDigits(amount, 2);
        return currency == "USD" ? $"${number}" : $"{number} {currency}";
    }

    /// <summary>Token counts: 412, 1.9k, 31k, 1.2M (one decimal below 10, as people read them).</summary>
    internal static string Tokens(long count) => count switch
    {
        < 1_000 => count.ToString(Invariant),
        // Up to 999,499: from 999,500 the k count would round to "1000k", so it's shown as 1M.
        < 999_500 => Short(count / 1_000m) + "k",
        _ => Short(count / 1_000_000m) + "M",
    };

    private static string Short(decimal value) =>
        value < 10 ? Math.Round(value, 1, MidpointRounding.AwayFromZero).ToString("0.#", Invariant)
            : Math.Round(value, 0, MidpointRounding.AwayFromZero).ToString("0", Invariant);

    private static string SignificantDigits(decimal amount, int digits)
    {
        int decimals = digits - 1 - (int)Math.Floor(Math.Log10((double)Math.Abs(amount)));
        decimals = Math.Clamp(decimals, 0, 6);
        return Math.Round(amount, decimals, MidpointRounding.AwayFromZero).ToString("0.######", Invariant);
    }

    private static string Requests(int count) => count == 1 ? "1 request" : $"{count} requests";

    /// <summary>The currency of the priced records; USD when none is priced (then no amount is shown anyway).</summary>
    internal static string CurrencyOf(IEnumerable<UsageRecord> records) =>
        records.FirstOrDefault(r => r.Cost is not null)?.Cost!.Currency ?? "USD";
}