using System.Text.Json;
using louis_agent.core.config;
using louis_agent.core.providers;
using louis_agent.core.tools;
using louis_agent.core.usage;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

// F1-S1: records land in {LOG_DIRECTORY}/usage-YYYY-MM.jsonl. F1-S4: no prompt text in the file.
public class JsonlUsageSinkTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp() => _directory = Directory.CreateTempSubdirectory("usage-ledger-").FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_directory, recursive: true);

    private static UsageRecord Record(DateTimeOffset at, UsageTokens? tokens = null) => new(
        At: at, Round: 1, Purpose: UsagePurpose.Turn, Host: null, Session: null, Turn: null, Task: null, Run: null,
        Service: null, Model: "claude-haiku-4-5-20251001", Tokens: tokens ?? new UsageTokens(39, null, 10_227, 206, null),
        DurationMs: 1_234, Stop: "stop");

    private string[] Lines(string fileName) => File.ReadAllLines(Path.Combine(_directory, fileName));

    [Test]
    public void Record_AppendsOneJsonLinePerRecord()
    {
        var sink = new JsonlUsageSink(_directory);
        var at = new DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);

        sink.Record(Record(at));
        sink.Record(Record(at.AddSeconds(5)));

        string[] lines = Lines("usage-2026-10.jsonl");
        Assert.That(lines, Has.Length.EqualTo(2));
        Assert.That(JsonSerializer.Deserialize<UsageRecord>(lines[0], JsonlUsageSink.Json), Is.EqualTo(Record(at)));
    }

    [Test]
    public void Record_UsesTheSpecsFieldNames()
    {
        new JsonlUsageSink(_directory).Record(Record(new DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.Zero)));

        using var json = JsonDocument.Parse(Lines("usage-2026-10.jsonl").Single());
        JsonElement root = json.RootElement;
        Assert.That(root.GetProperty("duration_ms").GetInt64(), Is.EqualTo(1_234));
        Assert.That(root.GetProperty("tokens").GetProperty("cache_read").GetInt64(), Is.EqualTo(10_227));
        Assert.That(root.GetProperty("purpose").GetString(), Is.EqualTo("turn"));
    }

    [Test]
    public void Record_WithCost_WritesTheSpecsCostShape()
    {
        var at = new DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);
        new JsonlUsageSink(_directory).Record(Record(at) with { Cost = new UsageCost("USD", 0.036503m, "2026-10-10") });

        using var json = JsonDocument.Parse(Lines("usage-2026-10.jsonl").Single());
        JsonElement cost = json.RootElement.GetProperty("cost");
        Assert.That(cost.GetProperty("currency").GetString(), Is.EqualTo("USD"));
        Assert.That(cost.GetProperty("amount").GetDecimal(), Is.EqualTo(0.036503m));
        Assert.That(cost.GetProperty("price_table").GetString(), Is.EqualTo("2026-10-10"));
    }

    [Test]
    public void Record_LineWrittenBeforeF2_ReadsBackWithNoCost()
    {
        // Ledgers written before prices existed have no "cost"; they must still read back, unpriced.
        const string line = """{"at":"2026-10-10T17:43:20+00:00","round":1,"purpose":"turn","host":"api","session":"sess_1","turn":1,"task":null,"run":null,"service":null,"model":"claude-haiku-5-5","tokens":{"input":47605,"cache_write":null,"cache_read":0,"output":470,"reasoning":null},"duration_ms":3730,"stop":"tool_calls"}""";

        UsageRecord record = JsonSerializer.Deserialize<UsageRecord>(line, JsonlUsageSink.Json)!;

        Assert.That(record.Cost, Is.Null);
        Assert.That(record.Tokens.Input, Is.EqualTo(47_605));
    }

    [Test]
    public void Record_FileNamedByRecordMonth()
    {
        var sink = new JsonlUsageSink(_directory);

        sink.Record(Record(new DateTimeOffset(2026, 9, 30, 23, 59, 0, TimeSpan.Zero)));
        sink.Record(Record(new DateTimeOffset(2026, 10, 1, 0, 1, 0, TimeSpan.Zero)));

        Assert.That(Directory.GetFiles(_directory).Select(Path.GetFileName).Order(),
            Is.EqualTo(new[] { "usage-2026-09.jsonl", "usage-2026-10.jsonl" }));
    }

    [Test]
    public void Record_MonthIsTakenInUtc()
    {
        // 00:30 on 1 October in UTC+2 is still September in UTC; every host must agree on which file a record is in.
        new JsonlUsageSink(_directory).Record(Record(new DateTimeOffset(2026, 10, 1, 0, 30, 0, TimeSpan.FromHours(2))));

        Assert.That(File.Exists(Path.Combine(_directory, "usage-2026-09.jsonl")));
    }

    [Test]
    public void Record_NullCountsWrittenAsNull()
    {
        new JsonlUsageSink(_directory).Record(Record(DateTimeOffset.UtcNow, new UsageTokens(null, null, null, null, null)));

        using var json = JsonDocument.Parse(Lines(JsonlUsageSink.FileNameFor(DateTimeOffset.UtcNow)).Single());
        JsonElement tokens = json.RootElement.GetProperty("tokens");
        Assert.That(tokens.GetProperty("input").ValueKind, Is.EqualTo(JsonValueKind.Null), "null means not reported; never 0");
        Assert.That(tokens.GetProperty("cache_write").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public void Record_RaisesUsageRecorded()
    {
        var sink = new JsonlUsageSink(_directory);
        var seen = new List<UsageRecord>();
        sink.UsageRecorded += seen.Add;

        UsageRecord record = Record(DateTimeOffset.UtcNow);
        sink.Record(record);

        Assert.That(seen, Is.EqualTo(new[] { record }));
    }

    [Test]
    public void Record_DirectoryCannotBeWritten_WarnsAndDoesNotThrow()
    {
        // A file where the directory should be: the ledger must never break the model call.
        string notADirectory = Path.Combine(_directory, "file");
        File.WriteAllText(notADirectory, "");
        var sink = new JsonlUsageSink(notADirectory);

        Assert.DoesNotThrow(() => sink.Record(Record(DateTimeOffset.UtcNow)));
    }

    [Test]
    public async Task AgentHostBuild_RecordsToTheLedgerWithTheProvidersMapper()
    {
        // End to end: the factory's mapper reaches the engine, and a turn lands in the file. Cache writes only map with
        // the Anthropic mapper, so a cache_write in the file proves which one was used.
        const string marker = "PROMPT-MARKER-91c2";
        var usage = new UsageDetails
        {
            InputTokenCount = 10_266,
            OutputTokenCount = 144,
            AdditionalCounts = new AdditionalPropertiesDictionary<long> { ["CacheCreationInputTokens"] = 10_227 },
        };
        var factory = new StubClientFactory(
            new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "hi")) { Usage = usage }),
            new AnthropicUsageMapper());
        AgentOptions agent = TestPaths.Agent();
        agent.LogDirectory = _directory;

        AgentEngine engine = AgentHost.Build(new LlmOptions(), agent, factory).Engine;
        await engine.ProcessPromptAsync(engine.NewHistory(), $"say hi {marker}");

        string line = Lines(JsonlUsageSink.FileNameFor(DateTimeOffset.UtcNow)).Single();
        using var json = JsonDocument.Parse(line);
        Assert.That(json.RootElement.GetProperty("tokens").GetProperty("cache_write").GetInt64(), Is.EqualTo(10_227));
        Assert.That(json.RootElement.GetProperty("tokens").GetProperty("input").GetInt64(), Is.EqualTo(39));
        Assert.That(line, Does.Not.Contain(marker), "F1-S4: no prompt text in the ledger");
    }

    [Test]
    public void CreateUsageSink_FollowsUsageLedgerAndLogDirectory()
    {
        Assert.That(AgentHost.CreateUsageSink(new AgentOptions { LogDirectory = _directory }), Is.TypeOf<JsonlUsageSink>());
        Assert.That(AgentHost.CreateUsageSink(new AgentOptions { LogDirectory = _directory, UsageLedger = false }), Is.Null);
        Assert.That(AgentHost.CreateUsageSink(new AgentOptions { LogDirectory = null }), Is.Null);
    }

    [TestCase(null, true)]
    [TestCase("on", true)]
    [TestCase("off", false)]
    [TestCase("OFF", false)]
    public void UsageLedgerSetting_IsOnUnlessOff(string? value, bool expected)
    {
        var options = AgentOptions.FromEnvironment(name => name == "USAGE_LEDGER" ? value : null);

        Assert.That(options.UsageLedger, Is.EqualTo(expected));
    }

    private sealed class StubClientFactory(IChatClient client, IUsageMapper mapper) : ILlmClientFactory
    {
        public IChatClient Create(LlmOptions options) => client;
        public IUsageMapper CreateUsageMapper(LlmOptions options) => mapper;
    }
}