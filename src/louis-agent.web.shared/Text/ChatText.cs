namespace LouisAgent.Web.Shared.Text;

using LouisAgent.Web.Shared.Chat;

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

    /// <summary>What a running specialist is doing, per role (F34), in place of <see cref="Processing"/>; none by default.</summary>
    public IReadOnlyDictionary<string, RolePhases> RolePhases { get; init; } = new Dictionary<string, RolePhases>();
    public string Stopped { get; init; } = "Stopped";

    public string DelegatedTo { get; init; } = "Delegated to";

    /// <summary>A minion's card and Werk line (F30): the specialist handed one page to a minion.</summary>
    public string MinionLabel { get; init; } = "Minion";

    /// <summary>Between a minion's role and its model: "puls-minion-web-price-lookup on ollama-puls".</summary>
    public string OnModel { get; init; } = "on";

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

    /// <summary>A chat's title when its first message is empty and no attachment names it (W2: ChatRunner).</summary>
    public string NewChat { get; init; } = "New chat";

    /// <summary>The page's notices and a turn's errors, as the pages wrote them (W2: ChatRunner). {0} is the file's name.</summary>
    public string AttachmentTooLarge { get; init; } = "{0} is larger than 1 MB and was not attached.";
    public string SessionGone { get; init; } = "This chat's session no longer exists on the server (the API was restarted). Start a new chat to continue.";
    public string NeedsKey { get; init; } = "The API needs a key: enter AGENT_API_KEY under API key in the sidebar.";
    public string StillAnswering { get; init; } = "This chat is still answering; wait for it or press Stop.";
    public string ApiUnreachable { get; init; } = "Can't reach the agent API. Is the api container running?";

    public string Currency { get; init; } = "Currency";
    public string AsCharged { get; init; } = "As charged";
    /// <summary>{0} the base currency, {1} the rate, {2} the shown currency, {3} the rates' date.</summary>
    public string ConvertedAt { get; init; } = "1 {0} = {1} {2} on {3}; the ledger keeps what was charged";

    /// <summary>"3 tool calls, 1 thought" in this language.</summary>
    public string Steps(int tools, int thoughts) =>
        $"{tools} {(tools == 1 ? ToolCallOne : ToolCallMany)}, {thoughts} {(thoughts == 1 ? ThoughtOne : ThoughtMany)}";
}
