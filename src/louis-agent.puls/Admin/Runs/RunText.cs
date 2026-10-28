using LouisAgent.Web.Shared.Usage;

namespace LouisAgent.Puls.Admin.Runs;

/// <summary>
/// How the portal writes a run: its id where a full one is too long, its status, its repeats, its money, and a
/// question that has to fit in a table cell.
/// </summary>
public static class RunText
{
    private const int ShortLength = 8;
    private const int BlurbLength = 70;

    /// <summary>The first characters of an id: enough to tell two runs apart on a line.</summary>
    public static string Short(string id) => id.Length <= ShortLength ? id : id[..ShortLength];

    /// <summary>A run is running or done; anything else it ended with counts as failed.</summary>
    public static string Status(string status) => status switch
    {
        "running" => "running",
        "done" => "done",
        _ => "failed"
    };

    /// <summary>"1 repeat" or "3 repeats".</summary>
    public static string Repeats(int repeats) => repeats == 1 ? "1 repeat" : $"{repeats} repeats";

    /// <summary>
    /// A cost from the ledger (in dollars) in the browser's currency, as the chat shows it (F14); a dash when nobody
    /// knew the price.
    /// </summary>
    public static string Money(decimal? cost, CurrencyChoice? currency) =>
        cost is { } c ? UsageFormat.Money(c, "USD", currency) : "—";

    /// <summary>A question short enough for its cell: whole up to 70 characters, else cut with an ellipsis.</summary>
    /// <remarks>Guard clauses, one case per line, instead of a ternary inside a ternary.</remarks>
    public static string Blurb(string? question)
    {
        if (string.IsNullOrEmpty(question)) return "";
        if (question.Length <= BlurbLength) return question;
        return question[..(BlurbLength - 1)].TrimEnd() + "…";
    }
}
