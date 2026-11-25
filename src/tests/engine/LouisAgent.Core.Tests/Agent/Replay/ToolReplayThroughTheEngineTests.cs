using LouisAgent.Choices;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Skills.Providers;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Testing.Replay;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent.Replay;

/// <summary>
/// FX235: an engine that keeps recordings plays back what its tools answered. A turn that reads a file is recorded,
/// the file is then deleted, and the free replay still answers as the live run did, the model never asked: the tool's
/// answer came from the recording, not from the file, which is gone.
/// </summary>
public class ToolReplayThroughTheEngineTests
{
    /// <summary>The trial the test records into and replays from.</summary>
    private const string Trial = "r1:a:1/t1";

    /// <summary>A fresh replay folder for each test.</summary>
    private ReplayFolder _folder = null!;

    /// <summary>Makes the test's replay folder.</summary>
    [SetUp]
    public void SetUp() => _folder = new ReplayFolder();

    /// <summary>Deletes the test's replay folder.</summary>
    [TearDown]
    public void TearDown() => _folder.Dispose();

    /// <summary>An engine with the files toolset over this model, keeping recordings in the test's store.</summary>
    private AgentEngine Engine(IChatClient client) =>
        new(new MarkdownSkillProvider(TestPaths.SampleSkills), client, TestPaths.Agent(),
            toolsets: TestPaths.Toolsets(TestPaths.Agent(), [TestToolset.Files.Wire()]),
            replay: _folder.Store);

    /// <summary>The session's replay for the duration of a block.</summary>
    private static IDisposable Flow(WebReplayMode web, bool model) =>
        SessionReplay.Use(new SessionReplay(web, Trial, model));

    /// <summary>One streamed turn, its text.</summary>
    private static async Task<string> TurnAsync(AgentEngine engine)
    {
        var history = engine.StartHistory();
        var text = new System.Text.StringBuilder();
        using (UsageScope.Begin(host: "test", session: "s1", turn: 1))
            await foreach (var update in engine.StreamPromptAsync(history, "what is in the file?"))
                text.Append(update.Text);
        return text.ToString();
    }

    /// <summary>The model's call to read this file.</summary>
    private static ChatResponse Read(string relative) =>
        new(new ChatMessage(ChatRole.Assistant,
        [
            new FunctionCallContent("call_1", "ReadWorkspaceFile",
                new Dictionary<string, object?> { ["relativePath"] = relative })
        ]));

    /// <summary>The model's answer in words.</summary>
    private static ChatResponse Say(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    /// <summary>A tool's recorded answer is played back even when the file it read is gone.</summary>
    [Test]
    public async Task AToolsAnswer_IsPlayedBack_WhenWhatItReadIsGone()
    {
        string relative = $"replay-{Guid.NewGuid():N}.txt";
        string path = Path.Combine(TestPaths.RepoRoot, relative);
        File.WriteAllText(path, "the file as it was");

        // The live run reads the file and records the model's answers and the tool's.
        var live = new FakeChatClient(_ => Read(relative), _ => Say("It says what it was."));
        string recorded;
        try
        {
            using (Flow(WebReplayMode.Record, model: false))
                recorded = await TurnAsync(Engine(live));
        }
        finally
        {
            File.Delete(path);
        }

        // The file is gone: a replay that ran the tool again would hand the model "no such file", and miss.
        var throwing = new FakeChatClient(_ => throw new InvalidOperationException("the model was called"));
        string replayed;
        using (Flow(WebReplayMode.Replay, model: true))
            replayed = await TurnAsync(Engine(throwing));

        Assert.That(replayed, Is.EqualTo(recorded));
        Assert.That(throwing.Requests, Is.Empty);
    }
}
