using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Reports;

namespace LouisAgent.Orchestrator.Comms;

/// <summary>
/// The small pieces of Markdown the comms log is built from: a table cell, a quote, a code span, a usage figure. Table
/// formatting, not messages, so it stays here rather than in a message enum (ADR 0009).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
internal static class CommsMarkdown
{
    /// <summary>How long a tool's input or result may be in a table cell.</summary>
    public const int MaxCellChars = 160;

    /// <summary>A value that fits in one table cell: one line, pipes escaped, shortened.</summary>
    public static string Cell(string value, int max = MaxCellChars)
    {
        string flat = value.ReplaceLineEndings(" ⏎ ").Replace("|", "\\|").Replace("`", "'").Trim();
        return flat.Length <= max ? flat : flat[..max] + "…";
    }

    /// <summary>Text as a Markdown quote, blank lines kept.</summary>
    public static string Quote(string text) =>
        string.Join("\n",
            text.ReplaceLineEndings("\n").Split('\n').Select(line => line.Length == 0 ? ">" : "> " + line));

    /// <summary>A value as code, or "-" when there is none.</summary>
    public static string Code(string? value) => value is null ? "-" : $"`{value}`";

    /// <summary>One named part of a FIX-RESULT line, or nothing when louis-agent left it out.</summary>
    public static string Field(string name, string? value) => value is null ? "" : $" · {name} `{value}`";

    /// <summary>An exception's type without its namespace, as people say it.</summary>
    public static string ShortType(string type) => type[(type.LastIndexOf('.') + 1)..];

    /// <summary>What louis-agent said about its fix in one cell: its status, and its tests when it ran them.</summary>
    public static string FixSaid(FixResult? fix)
    {
        if (fix is null) return "no FIX-RESULT";
        if (fix.Tests is null) return fix.Status;
        return $"{fix.Status}, tests {fix.Tests}";
    }

    /// <summary>The FIX-RESULT line under a reply: every part louis-agent gave, or that it gave none.</summary>
    public static string FixResultLine(FixResult? result) =>
        result is null
            ? "FIX-RESULT: **none in this reply**"
            : $"FIX-RESULT: status **{result.Status}**{Field("commit", result.Commit)}" +
              $"{Field("branch", result.Branch)}{Field("tests", result.Tests)}{Field("reason", result.Reason)}";

    /// <summary>A usage figure in full (requests, tokens, cost), or "-" when it wasn't measured.</summary>
    public static string Usage(UsageTotal? usage, string currency, DisplayCurrency? display) =>
        usage is null ? "-" : UsageText.Describe(usage, currency, display);

    /// <summary>Just the tokens of a usage figure, or "-".</summary>
    public static string Tokens(UsageTotal? usage) =>
        usage is null ? "-" : UsageText.Tokens(usage.Input + usage.CacheWrite + usage.CacheRead + usage.Output);

    /// <summary>Just the cost of a usage figure, or "-".</summary>
    public static string Cost(UsageTotal? usage, string currency, DisplayCurrency? display) =>
        usage is null ? "-" : UsageText.Cost(usage, currency, display);
}
