using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LouisAgent.Api.Testing;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Llm;
using LouisAgent.SessionStream.Wire.Requests;
using Microsoft.AspNetCore.TestHost;

namespace LouisAgent.Api.IntegrationTests.Sessions;

/// <summary>
/// F171.1 step 10, the whole path, live: a host whose catalogue's function builds a made-up pack on the instance's
/// model is sent a made-up account as a photo, then as a PDF; the conversation's model hands the file's name to the
/// pack's tool, the tool reads it through its model, and the answer holds the account number while the ledger has
/// the reading's row, priced. A small model, each run well under a US cent; it skips without a key.
/// </summary>
[NonParallelizable]
[Category("Integration")]
public class ReadingThroughTheApiTests
{
    /// <summary>The small model both the conversation and the reading run on.</summary>
    private const string Model = "claude-haiku-5-5";

    /// <summary>The account number printed on the made-up account.</summary>
    private const string AccountNumber = "9876543210";

    private string _logs = null!;
    private string _skills = null!;

    [SetUp]
    public void SetUp()
    {
        _logs = Directory.CreateTempSubdirectory("reading-logs-").FullName;
        _skills = Directory.CreateTempSubdirectory("reading-skills-").FullName;
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(_logs, recursive: true);
        Directory.Delete(_skills, recursive: true);
    }

    /// <summary>
    /// The made-up account, sent as this file, is read by the pack's tool: the answer names its number, and the
    /// ledger has a priced row whose purpose is the tool's.
    /// </summary>
    [TestCase("sample-account.jpg", "image/jpeg")]
    [TestCase("sample-account.pdf", "application/pdf")]
    public async Task AnAccountSentAsAFile_IsReadByThePacksTool_AndPriced(string file, string mediaType)
    {
        string key = Outside.Key("ANTHROPIC_API_KEY", "the reading's model");
        await using var app = ApiTestHost.Build(host: Host(key));
        await app.StartAsync();
        var client = app.GetTestClient();

        string id = await OpenAsync(client);
        byte[] bytes = await File.ReadAllBytesAsync(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "Documents", file));
        var message = new SendMessageRequest(
            $"What is the account number on {file}?", [new AttachmentRequest(file, null, bytes, mediaType)]);
        using var answer = await client.PostAsJsonAsync($"/v1/sessions/{id}/messages", message);
        string text = WordsOf(await answer.Content.ReadAsStringAsync());

        var toolRows = LedgerRows().Where(row => row.GetProperty("purpose").GetString() == "tool").ToList();
        TestContext.Out.WriteLine($"{file}: {toolRows.Count} tool row(s)");
        Assert.Multiple(() =>
        {
            Assert.That(text, Does.Contain(AccountNumber));
            Assert.That(toolRows, Has.Count.EqualTo(1), "one reading, written as the tool's");
            Assert.That(toolRows.Select(Cost), Has.All.GreaterThan(0m), "the reading is priced");
        });
    }

    /// <summary>
    /// The host as a program whose pack needs a model builds it: the catalogue from its function, the ledger on and
    /// priced from the demo's table.
    /// </summary>
    private AgentHostContext Host(string key)
    {
        var llm = new LlmOptions
        {
            Provider = LlmOptions.Anthropic,
            Model = Model,
            ApiKey = key
        };
        var agent = new AgentOptions
        {
            WorkspaceRoot = TestPaths.RepoRoot,
            SkillsDirectory = _skills,
            RatesUrl = null,
            LogDirectory = _logs,
            UsageLedger = true,
            PricesFile = Path.Combine(TestPaths.RepoRoot, "demos", "louis-agent", "config", "prices.json")
        };
        return AgentHost.Build(llm, agent, new LlmClientFactory([AnthropicProvider.Instance]),
            models => new AgentCatalog([], [new AccountReaderPack(models.Instance)]));
    }

    /// <summary>Opens a session with the made-up pack's toolset, and gives back its id.</summary>
    private static async Task<string> OpenAsync(HttpClient client)
    {
        var opening = new CreateSessionRequest(null, null, null, [AccountReaderPack.PackName]);
        using var opened = await client.PostAsJsonAsync("/v1/sessions", opening);
        using var body = JsonDocument.Parse(await opened.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("session_id").GetString()!;
    }

    /// <summary>
    /// The words of a streamed answer, joined from its text deltas, so a number split across them reads whole.
    /// </summary>
    private static string WordsOf(string stream)
    {
        var words = new StringBuilder();
        foreach (string line in stream.Split('\n').Where(line => line.StartsWith("data: ", StringComparison.Ordinal)))
        {
            using var data = JsonDocument.Parse(line["data: ".Length..]);
            if (data.RootElement.TryGetProperty("delta", out var delta) &&
                delta.TryGetProperty("text", out var piece))
                words.Append(piece.GetString());
        }

        return words.ToString();
    }

    /// <summary>Every row the ledger wrote in this test's folder.</summary>
    private List<JsonElement> LedgerRows() =>
        Directory.EnumerateFiles(_logs, "usage-*.jsonl")
            .SelectMany(File.ReadAllLines)
            .Where(line => line.Length > 0)
            .Select(line => JsonDocument.Parse(line).RootElement.Clone())
            .ToList();

    /// <summary>A row's cost, or 0 when it has none.</summary>
    private static decimal Cost(JsonElement row) =>
        row.TryGetProperty("cost", out var cost) && cost.ValueKind == JsonValueKind.Object &&
        cost.TryGetProperty("amount", out var amount) && amount.ValueKind == JsonValueKind.Number
            ? amount.GetDecimal()
            : 0m;
}
