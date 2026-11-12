using LouisAgent.Api.Sessions;
using LouisAgent.Api.Sessions.Log;
using LouisAgent.Choices;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Usage.Records;
using LouisAgent.SessionStream.Wire.Requests;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Tests.Sessions.Log;

/// <summary>
/// F127.8, for louis-agent.dev's overview (Louis's pick, design A): every turn writes one line beside the ledger with
/// the page, the part of the site, who answered and the tools it called, and never the visitor's words.
/// </summary>
public class TurnLogTests
{
    private string _folder = "";

    /// <summary>A folder of its own for each test.</summary>
    [SetUp]
    public void MakeFolder()
    {
        _folder = Path.Combine(Path.GetTempPath(), "turn-log-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    /// <summary>The folder goes when the test is done.</summary>
    [TearDown]
    public void RemoveFolder() => Directory.Delete(_folder, recursive: true);

    /// <summary>A turn that calls a tool on a framework page, run as the endpoint runs it.</summary>
    private static async Task RunTurnAsync(TurnLog log)
    {
        var model = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [new FunctionCallContent("call_1", "ShowPage", new Dictionary<string, object?> { ["path"] = "/x" })])),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, [new TextContent("There it is.")])));
        var engine = new AgentEngine(new CompositeSkillProvider(), model, TestPaths.Agent(), toolsets: [],
            usageSink: new ListUsageSink());
        var session = new AgentSession("sess_log", engine, [TestToolset.Web.Wire()], 0, UsageTags.None)
        {
            Page = new PageRequest("/framework/packs", "Packs", "Every pack.", Focus: "framework"),
        };
        Assert.That(session.TryStartTurn(CancellationToken.None, out var turn), Is.True);

        await foreach (var _ in TurnStream.StreamTurnAsync(engine, session, "Where are the packs?", turn, log))
        {
        }
    }

    /// <summary>One line for the turn: its session, page, part and tools, and no words of the visitor's.</summary>
    [Test]
    public async Task ATurn_WritesOneLine_WithItsPagePartAndTools()
    {
        var log = new TurnLog(_folder);

        await RunTurnAsync(log);

        var lines = log.Read(DateOnly.FromDateTime(DateTime.UtcNow)).ToList();
        string file = File.ReadAllText(Directory.GetFiles(_folder, "turns-*.jsonl").Single());
        Assert.Multiple(() =>
        {
            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.That(lines[0] with
            {
                At = default,
                Tools = []
            }, Is.EqualTo(
                new TurnLogRecord(default, "sess_log", 1, null, "/framework/packs", "framework", [])));
            Assert.That(lines[0].Tools, Is.EqualTo(new[] { "ShowPage" }));
            Assert.That(file, Does.Not.Contain("Where are the packs"), "no visitor words are kept");
        });
    }

    /// <summary>Without a folder nothing is written, and reading finds nothing.</summary>
    [Test]
    public async Task WithoutAFolder_NothingIsWritten()
    {
        var log = new TurnLog(null);

        await RunTurnAsync(log);

        Assert.That(log.Read(DateOnly.FromDateTime(DateTime.UtcNow)), Is.Empty);
    }
}
