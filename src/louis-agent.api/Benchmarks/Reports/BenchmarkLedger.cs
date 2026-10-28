using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Usage.Reports;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Reports;

/// <summary>
/// Works out what an eval run cost from the usage ledger, never from the script. It picks the run's rows, drops the
/// sub-session roll-ups that would count a child twice, and places each row on the turn that caused it, climbing from
/// a minion or a specialist up to the Puls turn that sent it.
/// </summary>
/// <remarks>
/// A run's rows are those tagged with its id or <c>{id}:…</c>: the script tags its sessions
/// <c>{id}:{mode}:{repeat}</c>, and a specialist or a minion inherits its parent's tag. A child row names its parent
/// turn as <c>{session}#{turn}</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public static class BenchmarkLedger
{
    /// <summary>
    /// How far up a row may climb to find its turn: Puls, a specialist, a minion (ADR 0021: two levels down).
    /// </summary>
    private const int MaxDepth = 3;

    /// <summary>Reads the ledger's rows for every month in the window, skipping months that have no file yet.</summary>
    public static IEnumerable<UsageRecord> Read(string directory, DateTimeOffset from, DateTimeOffset to)
    {
        var month = new DateTime(from.UtcDateTime.Year, from.UtcDateTime.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        for (; month <= to.UtcDateTime; month = month.AddMonths(1))
        {
            string path = Path.Combine(directory, JsonlUsageSink.FileNameFor(month));
            if (!File.Exists(path)) continue;
            foreach (var record in UsageReport.Read(path)) yield return record;
        }
    }

    /// <summary>
    /// Picks out one run's rows, each model request once: the sub-session roll-ups would count a child twice.
    /// </summary>
    public static IReadOnlyList<UsageRecord> OfRun(string run, IEnumerable<UsageRecord> records) =>
        records.Where(r => r.Run is { } tag && (tag == run || tag.StartsWith(run + ":", StringComparison.Ordinal))
                                            && r.Purpose != UsagePurpose.SubSession)
            .ToArray();

    /// <summary>
    /// Turns the run's rows into requests and places each on its turn, using the sessions the script opened. A row
    /// nobody can place is kept, with no turn.
    /// </summary>
    public static IReadOnlyList<Request> Join(string run, IReadOnlyList<UsageRecord> ofRun,
        IReadOnlyDictionary<string, ConversationKey> sessions)
    {
        // Every child session's parent turn, from the child's own rows: a specialist's is a Puls turn, a minion's a
        // specialist's.
        var parents = new Dictionary<string, (string Session, int Turn)>();
        foreach (var record in ofRun)
            if (record.Session is { } session && ParentOf(record) is { } parent)
                parents.TryAdd(session, parent);

        // Climbs from a session up through its parents until it reaches one the script opened, which names the turn.
        TurnKey? TurnOf(string? session, int? turn)
        {
            for (int depth = 0; session is not null && turn is not null && depth < MaxDepth; depth++)
            {
                if (sessions.TryGetValue(session, out var conversation))
                    return new TurnKey(conversation.Mode, conversation.Repeat, conversation.Task, turn.Value);
                if (!parents.TryGetValue(session, out var up)) return null;
                (session, turn) = (up.Session, up.Turn);
            }

            return null;
        }

        return ofRun.Select(r => new Request(run, TurnOf(r.Session, r.Turn), r.At, r.Purpose, r.Session, r.Parent,
                r.Model,
                r.Tokens.Input, r.Tokens.Output, r.Tokens.CacheRead, r.Tokens.CacheWrite, r.Cost?.Amount, r.DurationMs))
            .OrderBy(r => r.At)
            .ToArray();
    }

    /// <summary>Adds up the run's cost and tokens the same way every host totals usage, so the numbers agree.</summary>
    public static Total TotalOf(IReadOnlyList<UsageRecord> ofRun)
    {
        var total = UsageReport.Total("run", ofRun);
        string currency = ofRun.Select(r => r.Cost?.Currency).FirstOrDefault(c => c is not null) ?? "USD";
        return new Total(total.Cost, currency, total.Input + total.CacheWrite + total.CacheRead + total.Output,
            total.Requests);
    }

    /// <summary>
    /// Lists the prompts the run used, as hashes, sorted so two runs on the same prompts give the same list. An agent
    /// the run never called leaves no hash, so compare agent by agent, not list against list.
    /// </summary>
    public static IReadOnlyList<string> PromptHashesOf(IReadOnlyList<UsageRecord> ofRun) =>
        ofRun.Select(r => r.PromptHash).OfType<string>().Distinct().Order(StringComparer.Ordinal).ToArray();

    /// <summary>Splits a child row's parent, <c>{session}#{turn}</c>, into the session and the turn.</summary>
    private static (string Session, int Turn)? ParentOf(UsageRecord record) =>
        record.Parent is { } parent && parent.LastIndexOf('#') is > 0 and var hash &&
        int.TryParse(parent.AsSpan(hash + 1), out int turn)
            ? (parent[..hash], turn)
            : null;
}
