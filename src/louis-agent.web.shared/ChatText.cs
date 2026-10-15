namespace LouisAgent.Web.Shared;

/// <summary>
/// The words the shared chat components show (TurnView, BlocksView, DelegationCard, CurrencySwitcher), so each app can
/// show them in its own language. English by default; an app registers its own copy (Puls: <c>PulsText.Afrikaans</c>).
/// Templates use numbered holes for <see cref="string.Format(string, object?[])"/>.
/// </summary>
public sealed record ChatText
{
    public string Thinking { get; init; } = "Thinking…";
    public string ThoughtFor { get; init; } = "Thought for {0}s";
    public string Input { get; init; } = "Input";
    public string Result { get; init; } = "Result";
    public string Processing { get; init; } = "PROCESSING";
    public string Stopped { get; init; } = "Stopped";

    public string DelegatedTo { get; init; } = "Delegated to";

    /// <summary>The chip on a continued sub-session (F26): a follow-up to a specialist that was already working.</summary>
    public string FollowUp { get; init; } = "follow-up";
    public string Specialist { get; init; } = "specialist";
    public string Task { get; init; } = "Task";
    public string Working { get; init; } = "Working";
    public string Work { get; init; } = "Work";
    public string Done { get; init; } = "Done";
    public string Failed { get; init; } = "Failed";
    public string Starting { get; init; } = "Starting";
    public string ToolCallOne { get; init; } = "tool call";
    public string ToolCallMany { get; init; } = "tool calls";
    public string ThoughtOne { get; init; } = "thought";
    public string ThoughtMany { get; init; } = "thoughts";

    public string Currency { get; init; } = "Currency";
    public string AsCharged { get; init; } = "As charged";
    /// <summary>{0} the base currency, {1} the rate, {2} the shown currency, {3} the rates' date.</summary>
    public string ConvertedAt { get; init; } = "1 {0} = {1} {2} on {3}; the ledger keeps what was charged";

    /// <summary>"3 tool calls, 1 thought" in this language.</summary>
    public string Steps(int tools, int thoughts) =>
        $"{tools} {(tools == 1 ? ToolCallOne : ToolCallMany)}, {thoughts} {(thoughts == 1 ? ThoughtOne : ThoughtMany)}";
}
