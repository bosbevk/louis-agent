using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Reports.Prompts;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Usage.Records;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// What the ledger gave a finished run: its total, its settings with the prompt hashes its rows carried, each agent's
/// prompts, its requests, and the lines for its log.
/// </summary>
internal sealed record LedgerJoinResult(
    Total? Total,
    IReadOnlyDictionary<string, string>? Settings,
    IReadOnlyList<Request> Requests,
    IReadOnlyList<string> Log,
    IReadOnlyList<AgentPrompt>? Prompts = null);

/// <summary>
/// Reads a run's requests and total from the ledger once the script has ended; a request's row is written when it
/// finishes, so the last turn's rows are there by then. A re-score that asked nothing cost a known 0 (not the null of
/// an unknown price, ADR 0007); one that asked a judge is priced from its own rows. A run with no ledger directory has
/// no total, and one whose ledger can't be read says so in its log.
/// </summary>
/// <remarks>
/// Taken out of <see cref="BenchmarkRecorder"/> (Fowler's Extract Class), which was past 200 lines: the recorder keeps
/// the flow of a run, this keeps what the ledger says about it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
internal static class LedgerJoin
{
    /// <summary>Everything the ledger knows about the run, joined to its turns through its sessions.</summary>
    public static LedgerJoinResult Of(BenchmarkRun run, string? rescoreOf, string? ledgerDirectory,
        IReadOnlyDictionary<string, string>? settings,
        IReadOnlyList<ModelInfo>? declared, IReadOnlyDictionary<string, ConversationKey> sessions)
    {
        var free = new LedgerJoinResult(new Total(0m, "USD", 0, 0), settings, [], []);
        if (ledgerDirectory is null) return rescoreOf is null ? new LedgerJoinResult(null, settings, [], []) : free;

        IReadOnlyList<UsageRecord> ofRun;
        try
        {
            ofRun = BenchmarkLedger.OfRun(run.Id,
                BenchmarkLedger.Read(ledgerDirectory, run.StartedAt, run.EndedAt ?? DateTimeOffset.UtcNow));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            // A ledger we can't read leaves the cost unknown, and the run's log says why.
            return new LedgerJoinResult(null, settings, [],
                [MessageText.Format(ApiUserText.BenchmarkLedgerUnreadable, new { Reason = ex.Message })]);
        }

        // A re-score asks the model nothing, unless a grader does (a judge): it has rows only then, and they are its
        // cost, shown as any run's.
        if (rescoreOf is not null && ofRun.Count == 0) return free;

        var requests = BenchmarkLedger.Join(run.Id, ofRun, sessions);
        // A row on a model the run didn't declare is named in the run's log, so a trend can't mix it in silently.
        var unlisted = RunModels.Unlisted(declared, requests.Select(r => r.Model));
        return new LedgerJoinResult(BenchmarkLedger.TotalOf(ofRun),
            BenchmarkSettings.WithPromptHashes(settings, BenchmarkLedger.PromptHashesOf(ofRun)), requests,
            unlisted.Count > 0
                ? [MessageText.Format(ApiUserText.BenchmarkModelUnlisted, new { Model = string.Join(", ", unlisted) })]
                : [],
            AgentPrompts.Of(run.Id, ofRun));
    }
}
