using System.Text.Json;
using louis_agent.core.tools;
using louis_agent.core.providers;
using Microsoft.Extensions.AI;

namespace louis_agent.core.tests;

public class AgentEngineTests
{
    private static ISkillProvider HelloSkills() =>
        new MarkdownSkillProvider(Path.Combine(TestPaths.RepoRoot, "Skills", "HelloWorld.md"));

    private static AgentEngine Engine(IChatClient client, bool supportsTools = true, string? paymoKey = null) =>
        new(HelloSkills(), client, TestPaths.Agent(paymoKey), supportsTools);

    [Test]
    public void Constructor_ExposesWorkspaceAndSkillTools()
    {
        var engine = Engine(new FakeChatClient());
        var names = engine.Tools.Select(t => t.Name).ToList();

        Assert.That(names, Does.Contain("ExecuteSkill"));
        Assert.That(names, Does.Contain("ReadWorkspaceFile"));
        Assert.That(names, Does.Not.Contain("LogTimeByTaskName"), "no Paymo key configured");
    }

    [Test]
    public void Constructor_RegistersUniqueToolNames()
    {
        // The Anthropic API rejects a request whose tool names collide.
        var engine = Engine(new FakeChatClient(), paymoKey: "test-key");
        var duplicates = engine.Tools.GroupBy(t => t.Name).Where(g => g.Count() > 1).Select(g => g.Key);

        Assert.That(duplicates, Is.Empty);
        Assert.That(engine.Tools.Select(t => t.Name), Does.Contain("DotNetBuild"));
    }

    [Test]
    public void PaymoTool_UsesExactParameterNames()
    {
        var engine = Engine(new FakeChatClient(), paymoKey: "test-key");
        var tool = engine.Tools.OfType<AIFunction>().Single(t => t.Name == "LogTimeByTaskName");

        string schema = tool.JsonSchema.GetRawText();
        foreach (string parameter in new[] { "taskName", "startTime", "endTime", "hours", "notes", "date" })
        {
            Assert.That(schema, Does.Contain($"\"{parameter}\""));
        }
    }

    [Test]
    public async Task ProcessPrompt_RunsToolCallLoopAndReturnsFinalAnswer()
    {
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("call_1", "ExecuteSkill",
                    new Dictionary<string, object?> { ["skillName"] = "SayHello", ["jsonArgs"] = "{\"name\":\"Agent\"}" })
            ])),
            messages =>
            {
                var toolResult = messages.Last().Contents.OfType<FunctionResultContent>().Single();
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, $"Skill said: {toolResult.Result}"));
            });
        var engine = Engine(client);
        var history = engine.NewHistory();

        string answer = await engine.ProcessPromptAsync(history, "say hello");

        Assert.That(answer, Does.Contain("Hello, Agent!"));
        Assert.That(client.Requests, Has.Count.EqualTo(2));
        Assert.That(client.Requests[0].Options!.Tools!.Select(t => t.Name), Does.Contain("ExecuteSkill"));
        Assert.That(history.Select(m => m.Role), Does.Contain(ChatRole.Tool), "tool result is kept in history");
    }

    [Test]
    public async Task ProcessPrompt_WithoutToolSupport_SendsNoToolsAndAddsNotice()
    {
        var client = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "I can't do that")));
        var engine = Engine(client, supportsTools: false);

        await engine.ProcessPromptAsync(engine.NewHistory(), "log time please");

        Assert.That(client.Requests[0].Options!.Tools, Is.Null.Or.Empty);
        Assert.That(client.Requests[0].Messages.Select(m => m.Text), Does.Contain(AgentEngine.NoToolsNotice));
    }

    [Test]
    public void ExecuteSkill_WithInvalidSkillName_ReturnsErrorMessage()
    {
        var result = Engine(new FakeChatClient()).ExecuteSkill("NonExistentSkill", "{}");

        Assert.That(result, Does.Contain("Error").And.Contain("not found"));
    }

    [Test]
    public void ExecuteSkill_WithInvalidJson_ReturnsErrorMessage()
    {
        var result = Engine(new FakeChatClient()).ExecuteSkill("SayHello", "invalid json {");

        Assert.That(result, Does.Contain("Invalid JSON"));
    }

    [Test]
    public void ExecuteSkill_WithEmptyJsonArgs_StillExecutes()
    {
        Assert.That(Engine(new FakeChatClient()).ExecuteSkill("GetCurrentTime", "{}"), Is.Not.Empty);
    }

    [Test]
    public void ExecuteSkill_TreatsArgumentsAsDataNotShellSyntax()
    {
        string marker = Path.Combine(Path.GetTempPath(), $"injected-{Guid.NewGuid():N}").Replace('\\', '/');
        try
        {
            string payload = $"x\"; touch '{marker}'; echo \"$(touch '{marker}')`touch '{marker}'`";
            string json = JsonSerializer.Serialize(new { name = payload });

            string result = Engine(new FakeChatClient()).ExecuteSkill("SayHello", json);

            Assert.That(File.Exists(marker), Is.False, "payload must not be executed");
            Assert.That(result, Does.Contain("touch"), "payload is echoed back literally");
        }
        finally
        {
            File.Delete(marker);
        }
    }

    [Test]
    public async Task ProcessPrompt_OversizedToolResult_IsSummarisedBeforeReachingModel()
    {
        string relative = $"big-{Guid.NewGuid():N}.txt";
        string path = Path.Combine(TestPaths.RepoRoot, relative);
        File.WriteAllText(path, new string('x', AgentEngine.MaxToolResultChars + 1));
        try
        {
            var client = new FakeChatClient(
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [
                    new FunctionCallContent("call_1", "ReadWorkspaceFile",
                        new Dictionary<string, object?> { ["relativePath"] = relative })
                ])),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "a file full of x")),
                messages =>
                {
                    var toolResult = messages.Last().Contents.OfType<FunctionResultContent>().Single();
                    return new ChatResponse(new ChatMessage(ChatRole.Assistant, toolResult.Result!.ToString()!));
                });
            var engine = Engine(client);

            string answer = await engine.ProcessPromptAsync(engine.NewHistory(), "what is in the file?");

            Assert.That(answer, Does.Contain("Summary:").And.Contain("a file full of x"));
            Assert.That(answer.Length, Is.LessThan(1_000), "raw output never reaches the main conversation");
            Assert.That(client.Requests[1].Options!.Tools, Is.Null, "summariser call has no tools");
            Assert.That(client.Requests[1].Messages.Last().Text, Does.Contain("what is in the file?"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task SummariseToolResult_WhenModelFails_FallsBackToTruncation()
    {
        var client = new FakeChatClient(_ => throw new HttpRequestException("boom"));
        var engine = Engine(client);
        string output = new('y', AgentEngine.MaxToolResultChars * 3);

        string result = await engine.SummariseToolResultAsync("DiffUnstaged", "{}", output, "show my changes");

        Assert.That(result, Does.Contain("too large").And.Contain("[truncated"));
        Assert.That(result.Length, Is.LessThan(AgentEngine.MaxToolResultChars + 500));
    }

    [Test]
    public async Task ProcessPrompt_ToolCallCutOffByOutputLimit_IsNotRunAndModelIsToldToChunk()
    {
        string relative = $"cut-off-{Guid.NewGuid():N}.html";
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("call_1", "WriteWorkspaceFile",
                    new Dictionary<string, object?> { ["relativePath"] = relative })
            ])) { FinishReason = ChatFinishReason.Length },
            messages =>
            {
                var toolResult = messages.Last().Contents.OfType<FunctionResultContent>().Single();
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, toolResult.Result!.ToString()!));
            });
        var engine = Engine(client);

        string answer = await engine.ProcessPromptAsync(engine.NewHistory(), "make me a web page");

        Assert.That(answer, Is.EqualTo(AgentEngine.TruncatedCallMessage));
        Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, relative)), Is.False, "the partial call never ran");
    }

    [Test]
    public async Task ProcessPrompt_ToolCallMissingArgument_TellsModelWhichOne()
    {
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("call_1", "WriteWorkspaceFile",
                    new Dictionary<string, object?> { ["relativePath"] = "never-written.txt" })
            ])),
            messages =>
            {
                var toolResult = messages.Last().Contents.OfType<FunctionResultContent>().Single();
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, toolResult.Result!.ToString()!));
            });
        var engine = Engine(client);

        string answer = await engine.ProcessPromptAsync(engine.NewHistory(), "write a file");

        Assert.That(answer, Does.Contain("content"), "detailed error names the missing parameter");
    }

    [Test]
    public async Task StreamPrompt_YieldsTextToolCallAndResultAsTheyHappen_AndRecordsHistory()
    {
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new TextContent("Let me run it. "),
                new FunctionCallContent("call_1", "ExecuteSkill",
                    new Dictionary<string, object?> { ["skillName"] = "SayHello", ["jsonArgs"] = "{\"name\":\"Agent\"}" })
            ])),
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "All done.")));
        var engine = Engine(client);
        var history = engine.NewHistory();

        var contents = new List<AIContent>();
        await foreach (var update in engine.StreamPromptAsync(history, "say hello"))
            contents.AddRange(update.Contents);

        Assert.That(contents.OfType<TextContent>().Select(t => t.Text), Is.EqualTo(new[] { "Let me run it. ", "All done." }));
        Assert.That(contents.OfType<FunctionCallContent>().Single().Name, Is.EqualTo("ExecuteSkill"));
        Assert.That(contents.OfType<FunctionResultContent>().Single().Result?.ToString(), Does.Contain("Hello, Agent!"));
        Assert.That(history.Last().Text, Is.EqualTo("All done."));
        Assert.That(history.Select(m => m.Role), Does.Contain(ChatRole.Tool), "tool result is kept in history");
    }

    [Test]
    public async Task StreamPrompt_ToolCallCutOffByOutputLimit_IsNotRun()
    {
        string relative = $"cut-off-{Guid.NewGuid():N}.html";
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [
                new FunctionCallContent("call_1", "WriteWorkspaceFile",
                    new Dictionary<string, object?> { ["relativePath"] = relative })
            ])) { FinishReason = ChatFinishReason.Length },
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "ok")));
        var engine = Engine(client);

        var results = new List<FunctionResultContent>();
        await foreach (var update in engine.StreamPromptAsync(engine.NewHistory(), "make me a web page"))
            results.AddRange(update.Contents.OfType<FunctionResultContent>());

        Assert.That(results.Single().Result, Is.EqualTo(AgentEngine.TruncatedCallMessage));
        Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, relative)), Is.False, "the partial call never ran");
    }

    [Test]
    public async Task StreamPrompt_ReplyCutOffByOutputLimit_AsksModelToContinue()
    {
        // A streamed tool call cut off mid-arguments is dropped, leaving only the text before it.
        var client = new FakeChatClient(
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Now I'll append the rest:"))
                { FinishReason = ChatFinishReason.Length },
            _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Done in smaller chunks.")));
        var engine = Engine(client);
        var history = engine.NewHistory();

        var text = new List<string>();
        await foreach (var update in engine.StreamPromptAsync(history, "write a big file"))
            text.AddRange(update.Contents.OfType<TextContent>().Select(t => t.Text));

        Assert.That(text, Is.EqualTo(new[] { "Now I'll append the rest:", "Done in smaller chunks." }));
        Assert.That(client.Requests[1].Messages.Last().Text, Is.EqualTo(AgentEngine.ContinueAfterCutOffMessage));
        Assert.That(history.Last().Text, Is.EqualTo("Done in smaller chunks."));
    }

    [Test]
    public async Task StreamPrompt_WithThinking_RequestsReasoningAndStreamsIt()
    {
        var client = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
            [new TextReasoningContent("Considering the request."), new TextContent("Answer.")])));
        var engine = new AgentEngine(HelloSkills(), client, TestPaths.Agent()) { Thinking = ReasoningEffort.Medium };

        var contents = new List<AIContent>();
        await foreach (var update in engine.StreamPromptAsync(engine.NewHistory(), "think about it"))
            contents.AddRange(update.Contents);

        Assert.That(client.Requests[0].Options!.Reasoning!.Effort, Is.EqualTo(ReasoningEffort.Medium));
        Assert.That(contents.OfType<TextReasoningContent>().Single().Text, Is.EqualTo("Considering the request."));
    }

    [Test]
    public async Task StreamPrompt_WithoutThinking_SendsNoReasoning()
    {
        var client = new FakeChatClient(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Answer.")));
        var engine = Engine(client);

        await foreach (var _ in engine.StreamPromptAsync(engine.NewHistory(), "hi")) { }

        Assert.That(client.Requests[0].Options!.Reasoning, Is.Null);
    }
}
