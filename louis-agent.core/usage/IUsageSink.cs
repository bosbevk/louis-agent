namespace louis_agent.core.usage;

/// <summary>Where usage records go. The JSONL ledger in production; a list in tests.</summary>
public interface IUsageSink
{
    /// <summary>Stores one record. Must never throw into the model call: a broken ledger only logs a warning.</summary>
    void Record(UsageRecord record);
}