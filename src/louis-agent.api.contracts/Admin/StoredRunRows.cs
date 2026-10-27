namespace LouisAgent.Api.Contracts.Admin;

/// <summary>
/// A stored run's whole read, the explorer's page: the run, its settings and modes, its log and notes, each mode's
/// roll-up, its conversations, how often each judge agreed with the people, its turns and its model requests.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record StoredRunReply(
    StoredRunRow Run,
    IReadOnlyDictionary<string, string>? Settings,
    IReadOnlyList<ModeRow> Modes,
    IReadOnlyList<string>? Log,
    IReadOnlyList<NoteRow> Notes,
    IReadOnlyList<SummaryRow>? Summaries,
    IReadOnlyList<ConversationRow> Conversations,
    IReadOnlyList<AgreementRow> Agreement,
    IReadOnlyList<TurnRow> Turns,
    IReadOnlyList<RequestRow> Requests);

/// <summary>
/// One repeat of a mode: what it opened its sessions with and, once done, the runner's rates for the repeats so far;
/// null on a mode from before them, never 0.
/// </summary>
public sealed record ModeRow(
    string Name,
    int Repeat,
    IReadOnlyList<string> Toolsets,
    string? Role,
    string? Llm,
    double? PassAtK,
    double? PassHatK,
    int? K,
    int? Infrastructure,
    int? Retried);

/// <summary>How often a judge agreed with the people on this run, as counts per class of the people's grades.</summary>
public sealed record AgreementRow(string Grader, CalibrationClassRow PersonPass, CalibrationClassRow PersonFail);

/// <summary>One turn's stream events, for its replay.</summary>
public sealed record TurnEventsReply(IReadOnlyList<TurnEventRow> Events);

/// <summary>One stream event: its type, its data as it was sent, and when it came.</summary>
public sealed record TurnEventRow(string Type, System.Text.Json.JsonElement Data, DateTimeOffset? At);
