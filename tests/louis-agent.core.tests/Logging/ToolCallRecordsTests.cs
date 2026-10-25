using LouisAgent.Core.Logging;
using LouisAgent.Core.Logging.Content;

namespace LouisAgent.Core.Tests.Logging;

// F18 step 4f.1: the two records keep what they are for (the tool, the sizes, the outcome) and hide what the call was given.
[NonParallelizable]
public class ToolCallRecordsTests
{
    private const string Arguments = "{\"query\":\"Malmesbury recipes\"}";
    private const string Request = "Ek is in Malmesbury, wat kan ek kook?";
    private string _directory = null!;

    [SetUp]
    public void SetUp() => _directory = Directory.CreateTempSubdirectory("records-").FullName;

    [TearDown]
    public void TearDown()
    {
        AgentLogging.Configure(null);
        Directory.Delete(_directory, recursive: true);
    }

    private (string Oversized, string Truncated) Written(LogContentMode mode, string? request = Request)
    {
        AgentLogging.Configure(_directory, mode);
        ToolCallRecords.OversizedToolResult("WebSearch", Arguments, 60_000, request, "summarised", 812);
        ToolCallRecords.TruncatedToolCall("WebSearch", Arguments, 16_000);
        AgentLogging.Configure(null);   // flushes and closes the files
        return (File.ReadAllText(Path.Combine(_directory, "oversized-tool-results.jsonl")),
            File.ReadAllText(Path.Combine(_directory, "truncated-tool-calls.jsonl")));
    }

    [Test]
    public void ByDefault_TheRecordsKeepTheToolAndTheSizes_AndNoArgumentValueNorWordOfTheRequest()
    {
        var (oversized, truncated) = Written(LogContentMode.Hashed);

        Assert.That(oversized, Does.Contain("WebSearch").And.Contain("60000").And.Contain("summarised").And.Contain("812"));
        Assert.That(truncated, Does.Contain("WebSearch").And.Contain("16000"));
        foreach (string file in new[] { oversized, truncated })
            Assert.That(file, Does.Not.Contain("Malmesbury").And.Not.Contain("recipes").And.Not.Contain("kook").And.Contain("#"));
        Assert.That(oversized, Does.Contain($"({Arguments.Length})").And.Contain($"({Request.Length})"), "the lengths are what is left");
    }

    [Test]
    public void UnderFull_TheRecordsAreAsToday()
    {
        var (oversized, truncated) = Written(LogContentMode.Full);

        Assert.That(oversized, Does.Contain("Malmesbury recipes").And.Contain("Ek is in Malmesbury, wat kan ek kook?"));
        Assert.That(truncated, Does.Contain("Malmesbury recipes"));
    }

    [Test]
    public void UnderFull_ARequestOverFiveHundredCharacters_IsCutAsToday()
    {
        var (oversized, _) = Written(LogContentMode.Full, new string('k', 600));

        Assert.That(oversized, Does.Contain(new string('k', 500) + "...").And.Not.Contain(new string('k', 501)));
    }

    [Test]
    public void ARecordWithNoRequest_StaysNull_InBothModes()
    {
        Assert.That(Written(LogContentMode.Hashed, request: null).Oversized, Does.Contain("null"));
        Assert.That(Written(LogContentMode.Full, request: null).Oversized, Does.Contain("null"));
    }
}
