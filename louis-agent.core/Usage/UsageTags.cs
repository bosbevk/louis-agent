namespace LouisAgent.Core.Usage;

/// <summary>
/// Ambient context for the requests of one turn, held in an <see cref="AsyncLocal{T}"/> so it flows across
/// <c>await</c> into the recording client without being passed through every call. A host opens one per turn;
/// disposing it restores the outer scope.
/// </summary>
/// <example>
/// From an ordinary async method (the CLI, the ACP server, the orchestrator):
/// <code>
/// using (UsageScope.Begin(UsagePurpose.Turn, host: "acp", session: id, turn: 3))
/// {
///     await foreach (var update in engine.StreamPromptAsync(...)) { ... }
/// }
/// </code>
/// From an async iterator (the API's SSE stream), where a scope it opens is lost at its first <c>yield</c>, activate the
/// scope around each step of the engine's stream instead:
/// <code>
/// using var usage = UsageScope.Begin(host: "api", session: id, turn: 3);
/// using (usage.Activate()) more = await updates.MoveNextAsync();
/// </code>
/// </example>
/// <summary>
/// Who the work is for, beyond the session: the task (e.g. "fix:e8b0b572223d"), the orchestrator run it belongs to, and
/// the service. Lets a ledger total a fix or a whole run across both agents (F1-S3).
/// </summary>
/// <param name="Parent">
/// For a sub-session (F5-S5): the turn that opened it, <c>{session}#{turn}</c>, so a parent turn's records and its
/// children's can be added up together.
/// </param>
public sealed record UsageTags(string? Task, string? Run, string? Service, string? Parent = null)
{
    public static readonly UsageTags None = new(null, null, null);
}
