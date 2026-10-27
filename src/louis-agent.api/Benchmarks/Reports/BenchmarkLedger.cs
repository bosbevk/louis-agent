using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using LouisAgent.Core.Usage.Reports;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;
using static LouisAgent.Api.Benchmarks.Runs.BenchmarkProtocol;

namespace LouisAgent.Api.Benchmarks.Reports;

/// <summary>
/// What a benchmark run cost, from the usage ledger (F31): never from the script. The run's rows are those whose
/// <c>run</c> is its id or starts with <c>{id}:</c> (the script tags its sessions <c>{id}:{mode}:{repeat}</c>, and a
/// specialist or a minion inherits its parent's tag). A <see cref="UsagePurpose.SubSession"/> roll-up is skipped: it
/// repeats in the parent's turn what the child's own rows already say. Each row is then joined to its turn: Puls's by
/// its session and turn number, a child's by its <c>parent</c> (<c>{session}#{turn}</c>), followed up through a
/// specialist to the Puls turn that sent it.
/// </summary>
public static class BenchmarkLedger
{
    /// <summary>How many parents a row's chain may climb: Puls, a specialist, a minion (ADR 0021: two levels down).</summary>
    private const int MaxDepth = 3;

    /// <summary>The ledger's rows of every month from <paramref name="from"/> to <paramref name="to"/>; none when there's no file yet.</summary>
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

    /// <summary>The rows of <paramref name="run"/>, each model request once: without the sub-session roll-ups.</summary>
    public static IReadOnlyList<UsageRecord> OfRun(string run, IEnumerable<UsageRecord> records) =>
        records.Where(r => r.Run is { } tag && (tag == run || tag.StartsWith(run + ":", StringComparison.Ordinal))
                                            && r.Purpose != UsagePurpose.SubSession)
            .ToArray();

    /// <summary>
    /// The run's rows as requests, each with its turn when one is known. <paramref name="sessions"/> are the sessions the
    /// script opened, by the conversation each belongs to; a row nobody can place is kept, with no turn.
    /// </summary>
    public static IReadOnlyList<Request> Join(string run, IReadOnlyList<UsageRecord> ofRun,
        IReadOnlyDictionary<string, ConversationKey> sessions)
    {
        // Every child session's parent turn, from the child's own rows: a specialist's is a Puls turn, a minion's a specialist's.
        var parents = new Dictionary<string, (string Session, int Turn)>();
        foreach (var record in ofRun)
            if (record.Session is { } session && ParentOf(record) is { } parent)
                parents.TryAdd(session, parent);

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

    /// <summary>The run's total with <see cref="UsageReport.Total"/>, as every host totals usage (ADR 0007).</summary>
    public static Total TotalOf(IReadOnlyList<UsageRecord> ofRun)
    {
        var total = UsageReport.Total("run", ofRun);
        string currency = ofRun.Select(r => r.Cost?.Currency).FirstOrDefault(c => c is not null) ?? "USD";
        return new Total(total.Cost, currency, total.Input + total.CacheWrite + total.CacheRead + total.Output,
            total.Requests);
    }

    /// <summary>
    /// The distinct prompt hashes of the run's rows, sorted ordinally so two runs on the same prompts give byte-equal lists
    /// (F18 step 4c). Rows with none (from before 4c) are skipped. It is the set of prompts the run <i>used</i>, not what it has:
    /// an agent the run never called leaves no hash, so compare per agent, not list against list.
    /// </summary>
    public static IReadOnlyList<string> PromptHashesOf(IReadOnlyList<UsageRecord> ofRun) =>
        ofRun.Select(r => r.PromptHash).OfType<string>().Distinct().Order(StringComparer.Ordinal).ToArray();

    /// <summary><c>{session}#{turn}</c> as its parts; null for a row with no parent.</summary>
    private static (string Session, int Turn)? ParentOf(UsageRecord record) =>
        record.Parent is { } parent && parent.LastIndexOf('#') is > 0 and var hash &&
        int.TryParse(parent.AsSpan(hash + 1), out int turn)
            ? (parent[..hash], turn)
            : null;
}
