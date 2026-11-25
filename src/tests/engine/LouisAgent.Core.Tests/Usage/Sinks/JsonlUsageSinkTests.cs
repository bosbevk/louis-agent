using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Pricing.Prices;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Core.Usage.Sinks;
using System.Text.Json;
using LouisAgent.Core.Config;
using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Hosting;

namespace LouisAgent.Core.Tests.Usage.Sinks;

/// <summary>
/// The ledger's file: each record a JSON line in {LOG_DIRECTORY}/usage-YYYY-MM.jsonl, with no prompt text in it, and
/// older lines still read back.
/// </summary>
public class JsonlUsageSinkTests
{
    private string _directory = null!;

    /// <summary>Each test gets a ledger folder of its own.</summary>
    [SetUp]
    public void SetUp() => _directory = Directory.CreateTempSubdirectory("usage-ledger-").FullName;

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void TearDown() => Directory.Delete(_directory, recursive: true);

    /// <summary>A request's record made at the time given, with the tokens given or none.</summary>
    private static UsageRecord Record(DateTimeOffset at, UsageTokens? tokens = null) => new(
        At: at, Round: 1, Purpose: UsagePurpose.Turn, Host: null, Session: null, Turn: null, Task: null, Run: null,
        Service: null, Model: "claude-haiku-4-5-20251001",
        Tokens: tokens ?? new UsageTokens(39, null, 10_227, 206, null),
        DurationMs: 1_234, Stop: "stop");

    /// <summary>The lines of a ledger file in the test's folder.</summary>
    private string[] ReadLines(string fileName) => File.ReadAllLines(Path.Combine(_directory, fileName));

    /// <summary>Each record adds one JSON line.</summary>
    [Test]
    public void Record_AppendsOneJsonLinePerRecord()
    {
        var sink = new JsonlUsageSink(_directory);
        var at = new DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);

        sink.Record(Record(at));
        sink.Record(Record(at.AddSeconds(5)));

        string[] lines = ReadLines("usage-2026-10.jsonl");
        Assert.That(lines, Has.Length.EqualTo(2));
        Assert.That(JsonSerializer.Deserialize<UsageRecord>(lines[0], JsonlUsageSink.Json), Is.EqualTo(Record(at)));
    }

    /// <summary>A line uses the field names the ledger's spec sets.</summary>
    [Test]
    public void Record_UsesTheSpecsFieldNames()
    {
        new JsonlUsageSink(_directory).Record(Record(new DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.Zero)));

        using var json = JsonDocument.Parse(ReadLines("usage-2026-10.jsonl").Single());
        JsonElement root = json.RootElement;
        Assert.That(root.GetProperty("duration_ms").GetInt64(), Is.EqualTo(1_234));
        Assert.That(root.GetProperty("tokens").GetProperty("cache_read").GetInt64(), Is.EqualTo(10_227));
        Assert.That(root.GetProperty("purpose").GetString(), Is.EqualTo("turn"));
    }

    /// <summary>A priced record writes its cost in the spec's shape.</summary>
    [Test]
    public void Record_WithCost_WritesTheSpecsCostShape()
    {
        var at = new DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);
        new JsonlUsageSink(_directory).Record(Record(at) with { Cost = new UsageCost("USD", 0.036503m, "2026-10-10") });

        using var json = JsonDocument.Parse(ReadLines("usage-2026-10.jsonl").Single());
        JsonElement cost = json.RootElement.GetProperty("cost");
        Assert.That(cost.GetProperty("currency").GetString(), Is.EqualTo("USD"));
        Assert.That(cost.GetProperty("amount").GetDecimal(), Is.EqualTo(0.036503m));
        Assert.That(cost.GetProperty("price_table").GetString(), Is.EqualTo("2026-10-10"));
    }

    /// <summary>A line written before costs were recorded reads back with no cost.</summary>
    [Test]
    public void Record_LineWrittenBeforeF2_ReadsBackWithNoCost()
    {
        // Ledgers written before prices existed have no "cost"; they must still read back, unpriced.
        const string line =
            """{"at":"2026-10-10T17:43:20+00:00","round":1,"purpose":"turn","host":"api","session":"sess_1","turn":1,"task":null,"run":null,"service":null,"model":"claude-haiku-5-5","tokens":{"input":47605,"cache_write":null,"cache_read":0,"output":470,"reasoning":null},"duration_ms":3730,"stop":"tool_calls"}""";

        UsageRecord record = JsonSerializer.Deserialize<UsageRecord>(line, JsonlUsageSink.Json)!;

        Assert.That(record.Cost, Is.Null);
        Assert.That(record.Tokens.Input, Is.EqualTo(47_605));
    }

    /// <summary>A record with a prompt hash is the same line, with the hash added last.</summary>
    [Test]
    public void Record_WithAPromptHash_IsTheSameLinePlusTheHashLast()
    {
        var at = new DateTimeOffset(2026, 10, 25, 9, 0, 0, TimeSpan.Zero);
        string hash = new('a', 64);
        var sink = new JsonlUsageSink(_directory);

        sink.Record(Record(at));
        sink.Record(Record(at) with { PromptHash = hash });

        string[] lines = ReadLines("usage-2026-10.jsonl");
        Assert.That(lines[1], Is.EqualTo(lines[0][..^1] + $",\"prompt_hash\":\"{hash}\"}}"));
        Assert.That(lines[0], Does.Not.Contain("prompt_hash"));
    }

    /// <summary>A line written before prompt hashes reads back with none.</summary>
    [Test]
    public void Record_LineWrittenBeforeStep4c_ReadsBackWithANullPromptHash()
    {
        const string line =
            """{"at":"2026-10-10T17:43:20+00:00","round":1,"purpose":"turn","host":"api","session":"sess_1","turn":1,"task":null,"run":null,"service":null,"model":"claude-haiku-5-5","tokens":{"input":47605,"cache_write":null,"cache_read":0,"output":470,"reasoning":null},"duration_ms":3730,"stop":"tool_calls"}""";

        UsageRecord record = JsonSerializer.Deserialize<UsageRecord>(line, JsonlUsageSink.Json)!;

        Assert.That(record.PromptHash, Is.Null, "unknown, never \"\"");
    }

    /// <summary>A record goes in the file of the month it was made.</summary>
    [Test]
    public void Record_FileNamedByRecordMonth()
    {
        var sink = new JsonlUsageSink(_directory);

        sink.Record(Record(new DateTimeOffset(2026, 9, 30, 23, 59, 0, TimeSpan.Zero)));
        sink.Record(Record(new DateTimeOffset(2026, 10, 1, 0, 1, 0, TimeSpan.Zero)));

        Assert.That(Directory.GetFiles(_directory).Select(Path.GetFileName).Order(),
            Is.EqualTo(new[] { "usage-2026-09.jsonl", "usage-2026-10.jsonl" }));
    }

    /// <summary>The month is taken in UTC, whatever the record's own offset.</summary>
    [Test]
    public void Record_MonthIsTakenInUtc()
    {
        // 00:30 on 1 October in UTC+2 is still September in UTC; every host must agree on which file a record is in.
        new JsonlUsageSink(_directory).Record(Record(new DateTimeOffset(2026, 10, 1, 0, 30, 0, TimeSpan.FromHours(2))));

        Assert.That(File.Exists(Path.Combine(_directory, "usage-2026-09.jsonl")));
    }

    /// <summary>A count the provider didn't give is written as null, never 0.</summary>
    [Test]
    public void Record_NullCountsWrittenAsNull()
    {
        new JsonlUsageSink(_directory).Record(Record(DateTimeOffset.UtcNow,
            new UsageTokens(null, null, null, null, null)));

        using var json = JsonDocument.Parse(ReadLines(JsonlUsageSink.FileNameFor(DateTimeOffset.UtcNow)).Single());
        JsonElement tokens = json.RootElement.GetProperty("tokens");
        Assert.That(tokens.GetProperty("input").ValueKind, Is.EqualTo(JsonValueKind.Null),
            "null means not reported; never 0");
        Assert.That(tokens.GetProperty("cache_write").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    /// <summary>Writing a record tells whoever listens.</summary>
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

    /// <summary>A folder that can't be written to gets a warning, and nothing throws.</summary>
    [Test]
    public void Record_DirectoryCannotBeWritten_WarnsAndDoesNotThrow()
    {
        // A file where the directory should be: the ledger must never break the model call.
        string notADirectory = Path.Combine(_directory, "file");
        File.WriteAllText(notADirectory, "");
        var sink = new JsonlUsageSink(notADirectory);

        Assert.DoesNotThrow(() => sink.Record(Record(DateTimeOffset.UtcNow)));
    }

    /// <summary>A host built the usual way records to the ledger, reading usage with its provider's mapper.</summary>
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
        await engine.ProcessPromptAsync(engine.StartConversation(), $"say hi {marker}");

        string line = ReadLines(JsonlUsageSink.FileNameFor(DateTimeOffset.UtcNow)).Single();
        using var json = JsonDocument.Parse(line);
        Assert.That(json.RootElement.GetProperty("tokens").GetProperty("cache_write").GetInt64(), Is.EqualTo(10_227));
        Assert.That(json.RootElement.GetProperty("tokens").GetProperty("input").GetInt64(), Is.EqualTo(39));
        Assert.That(line, Does.Not.Contain(marker), "F1-S4: no prompt text in the ledger");
    }

    /// <summary>Whether there is a ledger, and where, follows USAGE_LEDGER and LOG_DIRECTORY.</summary>
    [Test]
    public void CreateUsageSink_FollowsUsageLedgerAndLogDirectory()
    {
        string noPrices = Path.Combine(_directory, "no-prices.json");
        Assert.That(UsageLedger.Open(new AgentOptions
        {
            LogDirectory = _directory,
            PricesFile = noPrices
        }),
            Is.TypeOf<PricingUsageSink>().With.Property(nameof(PricingUsageSink.Inner)).TypeOf<JsonlUsageSink>());
        Assert.That(UsageLedger.Open(new AgentOptions
        {
            LogDirectory = _directory,
            UsageLedger = false
        }), Is.Null);
        Assert.That(UsageLedger.Open(new AgentOptions { LogDirectory = null }), Is.Null);
    }

    /// <summary>USAGE_LEDGER is on unless it says off, in any case.</summary>
    [TestCase(null, true)]
    [TestCase("on", true)]
    [TestCase("off", false)]
    [TestCase("OFF", false)]
    public void UsageLedgerSetting_IsOnUnlessOff(string? value, bool expected)
    {
        var options = AgentOptions.FromEnvironment(name => name == "USAGE_LEDGER" ? value : null);

        Assert.That(options.UsageLedger, Is.EqualTo(expected));
    }

    /// <summary>A client factory that hands back the fake model and the mapper given.</summary>
    private sealed class StubClientFactory(IChatClient client, IUsageMapper mapper) : ILlmClientFactory
    {
        /// <summary>The fake model, whatever is asked for.</summary>
        public IChatClient Create(LlmOptions options) => client;

        /// <summary>The mapper the test gave.</summary>
        public IUsageMapper CreateUsageMapper(LlmOptions options) => mapper;
    }

    /// <summary>A record with a cache hit is the same line, with the hit added last.</summary>
    [Test]
    public void ACacheHit_IsTheSameLineWithCacheHitBeforeTheClosingBrace()
    {
        // F18 step 4e.2: a replayed answer is flagged; every other line stays byte-equal.
        var at = new DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.Zero);
        var sink = new JsonlUsageSink(_directory);
        sink.Record(Record(at));
        sink.Record(Record(at) with { CacheHit = true });

        string[] lines = ReadLines("usage-2026-10.jsonl");
        Assert.That(lines[1], Is.EqualTo(lines[0][..^1] + ",\"cache_hit\":true}"));
        Assert.That(lines[0], Does.Not.Contain("cache_hit"));
    }

    /// <summary>A line with no cache hit reads back with none.</summary>
    [Test]
    public void ALineWithoutCacheHit_ReadsBackWithNull()
    {
        string line = JsonSerializer.Serialize(Record(new DateTimeOffset(2026, 10, 10, 9, 0, 0, TimeSpan.Zero)),
            JsonlUsageSink.Json);

        Assert.That(JsonSerializer.Deserialize<UsageRecord>(line, JsonlUsageSink.Json)!.CacheHit, Is.Null);
        Assert.That(
            JsonSerializer.Deserialize<UsageRecord>(line[..^1] + ",\"cache_hit\":true}", JsonlUsageSink.Json)!.CacheHit,
            Is.True);
    }
}
