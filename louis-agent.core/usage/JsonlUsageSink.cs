namespace louis_agent.core.usage;

/// <summary>
/// Appends each record as one JSON line to <c>{LOG_DIRECTORY}/usage-YYYY-MM.jsonl</c> (monthly, append-only), and raises
/// <see cref="UsageRecorded"/> for hosts that show live totals (F3).
/// </summary>
public sealed class JsonlUsageSink : IUsageSink
{
    /// <summary>Raised after each record is written.</summary>
    public event Action<UsageRecord>? UsageRecorded;

    public void Record(UsageRecord record)
    {
        // TODO F1 step 6: write through AgentLog (add an AgentLog.RecordUsage that reuses AppendJsonLine, so locking and
        //   the no-directory no-op stay in one place), with the file name from record.At ("usage-2026-10.jsonl").
        //   Use snake_case JSON names to match the spec. Then raise UsageRecorded.
        //   Also: a USAGE_LEDGER on/off setting in AgentOptions, and a one-time [WARN] on Console.Error when
        //   LOG_DIRECTORY is unset.
        _ = UsageRecorded;
        throw new NotImplementedException();
    }
}