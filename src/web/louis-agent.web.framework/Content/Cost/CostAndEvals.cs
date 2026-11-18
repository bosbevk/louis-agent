using System.Globalization;
using LouisAgent.Web.Components.Site.Text;

namespace LouisAgent.Web.Framework.Content.Cost;

/// <summary>
/// What Cost and evals tells (F162.1, step 12): the ledger, the price table and one request worked through to rand,
/// each written from the engine's own summaries (<c>UsageLedger</c>, <c>CostCalculator</c>) and the coding demo's
/// <c>prices.json</c> and <c>rates.json</c>, which the page's test reads the worked request from.
/// </summary>
/// <remarks>
/// The page's lists as data, so the page only configures the framework's parts (ADR 0125).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public static class CostAndEvals
{
    /// <summary>One Claude Haiku 5.5 request, 2,000 tokens in and 500 out, at the dated prices and rate.</summary>
    public static readonly CostExample Example =
        new("claude-haiku-5-5", 2000, 500, 0.10m, 0.50m, "2026-10-10", 16.5331m, "2026-10-09");

    /// <summary>What the ledger is and keeps.</summary>
    public static readonly IReadOnlyList<Term> Ledger =
    [
        new(["usage-YYYY-MM.jsonl"], "A file a month in the log folder, one line for every request to a model."),
        new(["the tokens"], "Each kind on its own, in, out, read from the cache and written to it, so the sum is plain."),
        new(["the cost"], "Worked out as the line is written, and kept in US dollars, as it was charged."),
        new(["null"], "A model the price table doesn't list costs null, unknown, never nought.")
    ];

    /// <summary>What the price table holds.</summary>
    public static readonly IReadOnlyList<Term> PriceTable =
    [
        new(["prices.json"], "The price of a million tokens of each kind, for each model, with the date it was copied."),
        new(["as_of"], "A record keeps the prices it was written with, so a change of price never rewrites history."),
        new(["long_prompt"], "A higher price for the whole request once its prompt is over a number of tokens."),
        new(["llama", "qwen"], "Local models through Ollama cost nothing to run, so they are priced at nought on purpose.")
    ];

    /// <summary>The worked request, step by step, its numbers the example's own.</summary>
    public static IReadOnlyList<Step> TheRand =>
    [
        new("The tokens.", $"`{Example.Model}`, {N(Example.InputTokens)} tokens in and {N(Example.OutputTokens)} out."),
        new("The prices.", $"From `prices.json` of {Example.PricesAsOf}, ${Example.InputPerMillion} a million in and " +
            $"${Example.OutputPerMillion} a million out."),
        new("The dollars.", $"{N(Example.InputTokens)} times ${Example.InputPerMillion} and {N(Example.OutputTokens)} " +
            $"times ${Example.OutputPerMillion}, over a million, is {Example.DollarsText}, which the ledger keeps."),
        new("The rand.", $"At R{Example.RandPerDollar} a dollar, from `rates.json` of {Example.RatesAsOf}, it is " +
            $"{Example.RandText}. Live rates are used when they answer, and the file when they don't.")
    ];

    /// <summary>A count of tokens as the page writes it, with its thousands marked.</summary>
    private static string N(int count) => count.ToString("N0", CultureInfo.InvariantCulture);
}
