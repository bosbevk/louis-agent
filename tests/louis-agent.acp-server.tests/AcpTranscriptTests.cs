
using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Skills.Providers;
using Microsoft.Extensions.AI;

namespace LouisAgent.AcpServer.Tests;

// S (CL15), the ACP server's step 0: a recorded Rider conversation (a bad line, an unknown method, initialize,
// session/new, one prompt with a file attached that the model answers with a thought, a tool call and a reply) is played
// through AcpServer with the scripted model, and everything it writes to stdout is pinned to AcpTranscript.json: the
// proof the split by responsibility changes nothing the client sees. Session and message ids, which are new every
// run, are numbered; this machine's temporary folder is <tmp>.
public class AcpTranscriptTests
{
    private static readonly string GoldenFile = Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.acp-server.tests", "AcpTranscript.json");
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private string _tmp = null!;

    [SetUp]
    public void SetUp()
    {
        _tmp = Directory.CreateTempSubdirectory("acp-transcript-").FullName.Replace('\\', '/');
        File.WriteAllText(Path.Combine(_tmp, "notes.md"), "# Notes\n\nMelktert on Sunday.\n");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_tmp, recursive: true);

    /// <summary>The specialist's one tool in this conversation: a note by name, so the card is a "read".</summary>
    private sealed class NoteTools
    {
        [System.ComponentModel.Description("Reads a note by name")]
        public string ReadNote([System.ComponentModel.Description("The note's name")] string name) => $"note {name}: melktert, Sunday";
    }

    private static FakeChatClient Model() => new(
        _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
        [
            new TextReasoningContent("The user attached a note; the tool can read it."),
            new TextContent("Let me check the note."),
            new FunctionCallContent("call_1", "ReadNote", new Dictionary<string, object?> { ["name"] = "recipe" }),
        ])),
        _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "The note says: melktert on Sunday.")));

    private async Task<string> TranscriptAsync()
    {
        var model = Model();
        var engine = new AgentEngine(new CompositeSkillProvider(), model, TestPaths.Agent(), toolsets: [new NoteTools()]);
        var console = new ScriptedConsole();
        var server = new AcpServer(engine, usageInChat: false, console.Input, console.Output);
        Task run = server.RunAsync();

        console.Send("not json at all");
        await console.LinesAsync(1);
        console.Send("""{"jsonrpc":"2.0","id":1,"method":"session/nothing","params":{}}""");
        await console.LinesAsync(2);
        console.Send("""{"jsonrpc":"2.0","id":2,"method":"initialize","params":{"protocolVersion":1}}""");
        await console.LinesAsync(3);
        console.Send($$$"""{"jsonrpc":"2.0","id":3,"method":"session/new","params":{"cwd":"{{{_tmp}}}"}}""");
        var lines = await console.LinesAsync(4);
        string sessionId = JsonDocument.Parse(lines[3]).RootElement.GetProperty("result").GetProperty("sessionId").GetString()!;

        console.Send($$$"""
            {"jsonrpc":"2.0","id":4,"method":"session/prompt","params":{"sessionId":"{{{sessionId}}}","prompt":[
              {"type":"text","text":"What does the note say?"},
              {"type":"resource_link","uri":"file:///{{{_tmp}}}/notes.md","name":"notes.md"},
              {"type":"resource_link","uri":"file:///{{{_tmp}}}/Open.cs","name":"Open.cs","description":"File opened in the IDE"}
            ]}}
            """.ReplaceLineEndings(""));
        console.End();
        await run.WaitAsync(TimeSpan.FromSeconds(30));

        // What the client saw, and what the model was asked: the attached note read in, the file open in the IDE left out.
        return Normalise(console.Output.Lines, model.Requests[0].Messages.Last(m => m.Role == ChatRole.User).Text);
    }

    /// <summary>The transcript as pretty JSON, with the ids numbered by first appearance and the temp folder named.</summary>
    private string Normalise(IReadOnlyList<string> lines, string prompt)
    {
        var transcript = new { client = lines.Select(l => JsonDocument.Parse(l).RootElement).ToList(), prompt };
        string text = JsonSerializer.Serialize(transcript, Json);
        text = text.Replace(_tmp, "<tmp>");
        foreach (string prefix in new[] { "sess", "msg" })
        {
            var seen = new Dictionary<string, int>();
            text = Regex.Replace(text, prefix + "_[0-9a-f]{32}", m => $"{prefix}_{(seen.TryGetValue(m.Value, out int n) ? n : seen[m.Value] = seen.Count + 1)}");
        }
        return text.ReplaceLineEndings("\n") + "\n";
    }

    [Test]
    public async Task WhatRiderSees_MatchesTheGoldenFile()
    {
        string actual = await TranscriptAsync();
        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
        {
            File.WriteAllText(GoldenFile, actual);
            Assert.Pass($"Wrote {GoldenFile}; review its diff before committing.");
        }

        Assert.That(File.Exists(GoldenFile), $"No golden file; run with LOUIS_UPDATE_GOLDEN=1 to write {GoldenFile}.");
        string expected = File.ReadAllText(GoldenFile).ReplaceLineEndings("\n");
        Assert.That(actual, Is.EqualTo(expected),
            "What the ACP client sees changed. If that is intended, rerun with LOUIS_UPDATE_GOLDEN=1 and review the diff.");
    }
}
