namespace louis_agent.core.tests;

// F1-S1: records land in {LOG_DIRECTORY}/usage-YYYY-MM.jsonl. F1-S4: no prompt text in the file.
public class JsonlUsageSinkTests
{
    [Test]
    public void Record_AppendsOneJsonLinePerRecord()
    {
        Assert.Ignore("TODO F1 step 6");
    }

    [Test]
    public void Record_FileNamedByRecordMonth()
    {
        Assert.Ignore("TODO F1 step 6");
    }

    [Test]
    public void Record_NullCountsWrittenAsNull()
    {
        Assert.Ignore("TODO F1 step 6");
    }

    [Test]
    public void Record_RaisesUsageRecorded()
    {
        Assert.Ignore("TODO F1 step 6");
    }
}