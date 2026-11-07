using LouisAgent.Choices;

namespace LouisAgent.Web.Evals.Admin.RunPage;

/// <summary>One tab of the bar: which, its name, and the note under it saying what it holds.</summary>
public sealed record RunTabEntry(RunTab Tab, string Name, string Note);

/// <summary>
/// The run page's tabs: the one the address names (the summary when it names none or one that doesn't exist), and
/// the bar's entries in reading order, the answer first.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public static class RunTabs
{
    /// <summary>The bar, in reading order.</summary>
    public static readonly IReadOnlyList<RunTabEntry> All =
    [
        new(RunTab.Summary, "Summary", "results by category, pass rates, trends"),
        new(RunTab.Conversations, "Conversations", "the modes side by side: conversations, turns, requests"),
        new(RunTab.Judge, "Judge", "does the judge see what you see?"),
        new(RunTab.Notes, "Notes", "what changed, what you think explains it"),
        new(RunTab.Settings, "Settings", "the eval, its graders, the settings, the modes")
    ];

    /// <summary>The tab the address names, read once; the summary when it names none or one we don't have.</summary>
    public static RunTab Parse(string? query) =>
        SettingChoice.TryParse(query, out RunTab? tab) ? tab ?? RunTab.Summary : RunTab.Summary;
}
