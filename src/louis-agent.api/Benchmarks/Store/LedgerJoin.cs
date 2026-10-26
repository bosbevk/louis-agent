using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Usage.Records;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>What the ledger gave a finished run (F31, F18 step 5c.2): its total, its settings with the prompt hashes its rows carried, its requests, and the lines for its log.</summary>
internal sealed record LedgerJoinResult(Total? Total, IReadOnlyDictionary<string, string>? Settings, IReadOnlyList<Request> Requests, IReadOnlyList<string> Log);

/// <summary>
/// The run's requests and total from the ledger, read once the script has ended (taken out of <see cref="BenchmarkRecorder"/>, F18 step 5c.2, which was past 200 lines). A request's
/// row is written when it finishes, so the last turn's rows should be there by then. A re-score asked nothing: its total is a known 0, not the null of an unknown price and not the
/// stored run's rows (F18 step 5b.2, ADR 0007); one that asked a judge is priced from its own rows (step 6.1b). A run with no ledger directory has no total (unknown); one whose ledger can't be read says so in its log.
/// </summary>
internal static class LedgerJoin
{
    public static LedgerJoinResult Of(BenchmarkRun run, string? rescoreOf, string? ledgerDirectory, IReadOnlyDictionary<string, string>? settings,
        IReadOnlyList<ModelInfo>? declared, IReadOnlyDictionary<string, ConversationKey> sessions)
    {
        var free = new LedgerJoinResult(new Total(0m, "USD", 0, 0), settings, [], []);
        if (ledgerDirectory is null) return rescoreOf is null ? new LedgerJoinResult(null, settings, [], []) : free;

        IReadOnlyList<UsageRecord> ofRun;
        try
        {
            ofRun = BenchmarkLedger.OfRun(run.Id, BenchmarkLedger.Read(ledgerDirectory, run.StartedAt, run.EndedAt ?? DateTimeOffset.UtcNow));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return new LedgerJoinResult(null, settings, [], [MessageText.Format(ApiUserText.BenchmarkLedgerUnreadable, new { Reason = ex.Message })]);
        }

        // A re-score asks the model nothing, unless a grader does (a judge, F18 step 6.1b): it has rows only then, and they are its cost, shown as any run's.
        if (rescoreOf is not null && ofRun.Count == 0) return free;

        var requests = BenchmarkLedger.Join(run.Id, ofRun, sessions);
        // F18 step 2a: a row on a model the run didn't declare is named in the run's log, so a trend can't mix it in silently.
        var unlisted = RunModels.Unlisted(declared, requests.Select(r => r.Model));
        return new LedgerJoinResult(BenchmarkLedger.TotalOf(ofRun), BenchmarkSettings.WithPromptHashes(settings, BenchmarkLedger.PromptHashesOf(ofRun)), requests,
            unlisted.Count > 0 ? [MessageText.Format(ApiUserText.BenchmarkModelUnlisted, new { Model = string.Join(", ", unlisted) })] : []);
    }
}
