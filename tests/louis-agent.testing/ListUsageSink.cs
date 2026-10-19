using LouisAgent.Core.Usage;

namespace LouisAgent.Testing;

/// <summary>Collects usage records in memory for assertions.</summary>
public sealed class ListUsageSink : IUsageSink
{
    public List<UsageRecord> Records { get; } = [];

    public UsageRecord Record(UsageRecord record)
    {
        Records.Add(record);
        return record;
    }
}
