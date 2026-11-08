using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using System.ComponentModel;
using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Orchestrator.Comms;
using LouisAgent.Orchestrator.Messages;
using LouisAgent.Orchestrator.Tools.Records;
using LouisAgent.Packs.Triage.Feed;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Orchestrator.Tools;

/// <summary>
/// The orchestrator's tools for one service: the fix delegated to louis-agent, and the two decisions a runbook
/// allows instead. It never edits code, and what louis-agent reports is checked by <see cref="FixChecks"/>. Every
/// public method is a tool; what isn't a tool stays internal or private.
/// </summary>
/// <remarks>
/// Facade: the model sees one toolset, while the work behind each tool lives where it belongs: the fix conversation in
/// <see cref="FixConversation"/>, git in <see cref="ServiceRepository"/>, the jsonl lines in <see cref="StateFiles"/>
/// and the fix task in <see cref="FixPrompt"/>. The tools and their descriptions stay exactly as they were, so the
/// model's view doesn't change with the split (ADR 0034).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0034-code-a-person-can-maintain.md">ADR 0034: code a person can maintain</seealso>
internal sealed class ServiceTools(
    OrchestratorOptions options,
    JsonLinesErrorFeed feed,
    LouisAgentClient louisAgent,
    IReadOnlySet<string> methods,
    CommsLog comms)
{
    private static readonly ILogger Log = AgentLogging.For<ServiceTools>();
    private const int MaxFixFollowUps = 3;

    private readonly ServiceRepository _repository = new(options);
    private readonly StateFiles _files = new(options.StateDirectory);
    private readonly HashSet<string> _fixRequested = [];

    /// <summary>What the orchestrator says when a fix needs more rounds than one message allows.</summary>
    internal static string ContinueMessage => MessageText.Format(OrchestratorToolMessage.ContinueFix);

    /// <summary>
    /// The coding profile a fix session loads on louis-agent (F5-S4): files, git, dotnet and skills, nothing else, so
    /// every fix sends a smaller prefix and every fix session shares one cached engine.
    /// </summary>
    internal static readonly IReadOnlyList<string> FixToolsets =
        [FixToolset.DotNet.Wire(), FixToolset.Files.Wire(), FixToolset.Git.Wire(), FixToolset.Skills.Wire()];

    /// <summary>
    /// The current run's id, sent with every fix so louis-agent's usage records carry it. Internal on purpose: every
    /// public member of this class becomes a tool, property accessors included.
    /// </summary>
    internal string? RunId { get; set; }

    /// <summary>Tags for louis-agent's usage ledger: the error the fix is for, its run and its service.</summary>
    internal UsageTags FixTags(string errorId) => new($"fix:{errorId}", RunId, options.Service);

    /// <summary>
    /// Has louis-agent fix one error on its own branch, carrying on a few times if it runs out of rounds, and records
    /// how it went in fixes.jsonl. What louis-agent says is a claim the other tools check.
    /// </summary>
    [Description(
        "Asks louis-agent (the coding agent that owns this service's repository) to fix the exception with this " +
        "error id: it creates a fix branch, changes the code, adds a regression test, runs the tests and commits, then " +
        "reports back. Takes several minutes. Use it only when the method's runbook says the exception is auto-fixable, " +
        "at most once per error. Returns louis-agent's FIX-RESULT (status, commit, branch, tests, summary); that is a " +
        "claim, not proof, so confirm it afterwards with VerifyFixCommit, ReplayRequest and RunServiceTests.")]
    public async Task<string> CallLouisAgentFix(
        [Description("The error event's id, exactly as given in the event.")]
        string errorId,
        [Description(
            "The correct behaviour, taken from the method's runbook (what louis-agent should make the code do).")]
        string expectedBehaviour,
        CancellationToken cancellationToken = default)
    {
        if (feed.Find(errorId) is not { } error)
            return MessageText.Format(OrchestratorToolMessage.UnknownErrorId, new { ErrorId = errorId });
        if (!_fixRequested.Add(errorId))
            return MessageText.Format(OrchestratorToolMessage.FixAlreadyRequested, new { ErrorId = errorId });

        // The method comes from the error log, so only a known one goes into a branch name (ids are checked by
        // ErrorFeed). Every fix starts from main, so each branch holds one fix and can be merged on its own.
        string branch = $"fix/{(methods.Contains(error.Method) ? error.Method : "unknown")}-{error.Id}";
        if (ReturnToMain() is { } notReady)
            return MessageText.Format(OrchestratorToolMessage.FixCannotStart, new { Reason = notReady });

        Info(OrchestratorLogMessage.CallingLouisAgent,
            new
            {
                Url = options.LouisAgentUrl,
                error.ExceptionType,
                error.Method,
                Branch = branch
            });
        string sessionId = await louisAgent.CreateSessionAsync(FixTags(error.Id), cancellationToken, FixToolsets);
        try
        {
            var conversation = new FixConversation(louisAgent, comms, sessionId);
            AgentReply reply = await conversation.SendAsync(FixPrompt(error, expectedBehaviour, branch),
                cancellationToken);
            FixResult? result = LouisAgentClient.ParseFixResult(reply.Text);

            // A fix can need more tool rounds than one message allows; the session keeps the context, so ask it to
            // carry on.
            for (int i = 0; result is null && reply.Error is null && i < MaxFixFollowUps; i++)
            {
                Info(OrchestratorLogMessage.AskingToContinue);
                reply = await conversation.SendAsync(ContinueMessage, cancellationToken);
                result = LouisAgentClient.ParseFixResult(reply.Text);
            }

            var record = FixRecord.Of(errorId, conversation, reply, result);
            _files.Append(StateFiles.Fixes, record);
            LogFix(record);
            return JsonSerializer.Serialize(record);
        }
        finally
        {
            await louisAgent.DeleteSessionAsync(sessionId);
        }
    }

    /// <summary>Hands an error to a human team, and records it in escalations.jsonl.</summary>
    [Description(
        "Hands an error to a human team instead of fixing it: use it when the runbook says so (e.g. payments code " +
        "is never auto-fixed), when no runbook covers the method, or when a fix could not be confirmed. Records the " +
        "escalation for the team; returns a confirmation.")]
    public string EscalateToHuman(
        [Description("The error event's id.")] string errorId,
        [Description("The team named in the runbook (e.g. payments, orders).")]
        string team,
        [Description("Why this needs a human, and what is already known.")]
        string reason)
    {
        if (feed.Find(errorId) is not { } error)
            return MessageText.Format(OrchestratorToolMessage.UnknownErrorId, new { ErrorId = errorId });

        _files.Append(StateFiles.Escalations, EscalationRecord.Of(error, team, reason, DateTimeOffset.UtcNow));
        Info(OrchestratorLogMessage.Escalated, new
        {
            ErrorId = errorId,
            Team = team,
            Reason = reason
        });
        comms.Check("EscalateToHuman", $"**{team}**: {reason}");
        return MessageText.Format(OrchestratorToolMessage.Escalated, new
        {
            ErrorId = errorId,
            Team = team
        });
    }

    /// <summary>Records that an error is expected by its runbook, in acknowledged.jsonl.</summary>
    [Description("Records that an error needs no action because the runbook lists it as expected behaviour (e.g. a " +
                 "not-found or a correctly rejected request). Returns a confirmation.")]
    public string AcknowledgeError(
        [Description("The error event's id.")] string errorId,
        [Description("The runbook rule that makes this expected.")]
        string reason)
    {
        if (feed.Find(errorId) is null)
            return MessageText.Format(OrchestratorToolMessage.UnknownErrorId, new { ErrorId = errorId });

        _files.Append(StateFiles.Acknowledged, new AcknowledgementRecord(errorId, reason, DateTimeOffset.UtcNow));
        Info(OrchestratorLogMessage.Acknowledged, new
        {
            ErrorId = errorId,
            Reason = reason
        });
        comms.Check("AcknowledgeError", reason);
        return MessageText.Format(OrchestratorToolMessage.Acknowledged, new { ErrorId = errorId });
    }

    /// <summary>The task sent to louis-agent for one error (<see cref="Tools.FixPrompt"/>).</summary>
    internal string FixPrompt(ServiceError error, string expectedBehaviour, string branch) =>
        Tools.FixPrompt.For(options, error, expectedBehaviour, branch);

    /// <summary>Checks out main between fixes; nothing when it's checked out, otherwise why it couldn't be.</summary>
    internal string? ReturnToMain() => _repository.ReturnToMain();

    /// <summary>Logs how a fix went: with its commit and branch when louis-agent made one.</summary>
    private static void LogFix(FixRecord record)
    {
        if (record.Commit is null)
        {
            Info(OrchestratorLogMessage.LouisAgentReplied, new { record.Status });
            return;
        }

        Info(OrchestratorLogMessage.LouisAgentCommitted, new
        {
            record.Status,
            record.Commit,
            record.Branch
        });
    }

    /// <summary>Logs a step the orchestrator took, tagged so it reads apart from louis-agent's lines.</summary>
    private static void Info(OrchestratorLogMessage message, object? values = null) =>
        Log.Log(LogLevel.Information, message, values, tag: "orchestrator");
}
