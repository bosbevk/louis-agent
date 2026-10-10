using louis_agent.core.usage;

namespace louis_agent.core.tests;

// F1-S1: one record per request, streaming or not. F1-S4: no message text in a record.
public class UsageRecordingChatClientTests
{
    [Test]
    public void GetResponse_WritesOneRecordWithCountsAndScope()
    {
        // FakeChatClient returning a ChatResponse with Usage set; assert one record, its tokens, round 1 and purpose.
        Assert.Ignore("TODO F1 step 3");
    }

    [Test]
    public void GetResponse_TwoRequestsInOneScope_AreRounds1And2()
    {
        Assert.Ignore("TODO F1 step 3");
    }

    [Test]
    public void GetStreamingResponse_UsageInLastUpdate_WritesOneRecordWhenStreamEnds()
    {
        // FakeChatClient streams the response's contents, so put a UsageContent in the response message.
        Assert.Ignore("TODO F1 step 4");
    }

    [Test]
    public void GetStreamingResponse_Cancelled_RecordsWithStopCancelled()
    {
        Assert.Ignore("TODO F1 step 4");
    }

    [Test]
    public void Record_ContainsNoMessageText()
    {
        // Send a prompt containing a marker string; serialise the record and assert the marker is absent.
        Assert.Ignore("TODO F1 step 3");
    }
}

/// <summary>Collects records in memory for assertions.</summary>
internal sealed class ListUsageSink : IUsageSink
{
    public List<UsageRecord> Records { get; } = [];

    public void Record(UsageRecord record) => Records.Add(record);
}