using LouisAgent.Composition.Logging.Content;
using LouisAgent.Composition.Logging;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent;

/// <summary>The [TOOL] lines people watch in Rider and the logs: what ran and how it ended.</summary>
[NonParallelizable]
public class ToolCallLogTests
{
    /// <summary>One tool that answers pong.</summary>
    private sealed class PingTools
    {
        /// <summary>Answers pong to whoever is named.</summary>
        [System.ComponentModel.Description("Replies pong")]
        public string Ping(string to) => $"pong to {to}";
    }

    /// <summary>A tool call shows on stderr as its [TOOL] line going in and coming out.</summary>
    [Test]
    public async Task AToolCall_IsLoggedOnStderr_AsTool_ArrowIn_ArrowOut()
    {
        TextWriter original = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        try
        {
            AgentLogging.Configure(null, LogContentMode.Full); // the values, as a developer's own machine shows them
            var client = new FakeChatClient(
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
                    [new FunctionCallContent("call_1", "Ping", new Dictionary<string, object?> { ["to"] = "Louis" })])),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "done")));
            var skills = new MarkdownSkillProvider(TestPaths.SampleSkills);
            var engine = new AgentEngine(skills, client, TestPaths.Agent(), toolsets: [new PingTools()]);

            await engine.ProcessPromptAsync(engine.StartHistory(), "ping Louis");
        }
        finally
        {
            Console.SetError(original);
        }

        Assert.That(stderr.ToString(), Does.Contain("[TOOL] -> Ping({\"to\":\"Louis\"})"));
        Assert.That(stderr.ToString(), Does.Contain("[TOOL] <- Ping: pong to Louis"));
    }

    /// <summary>One tool that fails its first call and answers the next, so a log shows both endings.</summary>
    private sealed class PlaceTools
    {
        private bool _failed;

        /// <summary>Throws the first time it's asked, then finds the area.</summary>
        [System.ComponentModel.Description("Finds a place")]
        public string FindPlace(string area)
        {
            if (_failed) return $"found {area}";
            _failed = true;
            throw new InvalidOperationException($"No place called '{area}'.");
        }
    }

    /// <summary>
    /// The summary request sees the call's arguments whole, while the log line only shows their hash.
    /// </summary>
    [Test]
    public async Task TheSummaryRequest_KeepsTheFullArguments_WhileTheLogLineHashesThem()
    {
        // The model that summarises an oversized result needs to know what was asked; only what is logged is hashed.
        string relative = $"Malmesbury-{Guid.NewGuid():N}.txt";
        string path = Path.Combine(TestPaths.RepoRoot, relative);
        File.WriteAllText(path, new string('x', 50_001));
        TextWriter original = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        FakeChatClient client;
        try
        {
            AgentLogging.Configure(null);
            client = new FakeChatClient(
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
                [
                    new FunctionCallContent("call_1", "ReadWorkspaceFile",
                        new Dictionary<string, object?> { ["relativePath"] = relative })
                ])),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "a file full of x")),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "done")));
            var skills = new MarkdownSkillProvider(TestPaths.SampleSkills);
            var engine = new AgentEngine(skills, client, TestPaths.Agent(),
                toolsets: TestPaths.Toolsets(TestPaths.Agent(), [TestToolset.Files.Wire()]));

            await engine.ProcessPromptAsync(engine.StartHistory(), "Ek is in Malmesbury, wat kan ek kook?");
        }
        finally
        {
            Console.SetError(original);
            File.Delete(path);
        }

        string summaryRequest = client.Requests[1].Messages.Last().Text;
        Assert.That(summaryRequest, Does.Contain(relative), "the summary request carries the full arguments");
        Assert.That(stderr.ToString(), Does.Not.Contain("Malmesbury"));
        Assert.That(stderr.ToString(), Does.Contain("[TOOL] -> ReadWorkspaceFile(relativePath=#"));
    }

    /// <summary>By default a tool line shows what ran and how it ended, never what it was given.</summary>
    [Test]
    public async Task ByDefault_AToolLine_ShowsWhatRanAndHowItEnded_NeverWhatItWasGiven()
    {
        // The arguments, a failure's quoted reason and the result are a keyed hash and a length; the user's words (the
        // request is "Ek is in Malmesbury, wat kan ek kook?") appear nowhere, on stderr or in the run file.
        string directory = Directory.CreateTempSubdirectory("toollog-").FullName;
        TextWriter original = Console.Error;
        var stderr = new StringWriter();
        Console.SetError(stderr);
        string runFile;
        try
        {
            AgentLogging.Configure(directory);
            runFile = AgentLogging.RunFile!;

            FunctionCallContent Call(string id) =>
                new(id, "FindPlace", new Dictionary<string, object?> { ["area"] = "Malmesbury" });

            var client = new FakeChatClient(
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, [Call("call_1")])),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, [Call("call_2")])),
                _ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "done")));
            var skills = new MarkdownSkillProvider(TestPaths.SampleSkills);
            var engine = new AgentEngine(skills, client, TestPaths.Agent(), toolsets: [new PlaceTools()]);

            await engine.ProcessPromptAsync(engine.StartHistory(), "Ek is in Malmesbury, wat kan ek kook?");
        }
        finally
        {
            Console.SetError(original);
            AgentLogging.Configure(null); // flushes and closes the run file
        }

        string file = File.ReadAllText(runFile);
        foreach (string log in new[] { stderr.ToString(), file })
        {
            Assert.That(log, Does.Not.Contain("Malmesbury").And.Not.Contain("kook"));
            Assert.That(log, Does.Contain("[TOOL] -> FindPlace(area=#").And.Contain("(10)"));
            Assert.That(log, Does.Contain("[TOOL] <- FindPlace: #"), "the result is a hash and a length too");
            Assert.That(log, Does.Contain("InvalidOperationException"), "the failure still says what kind it was");
        }

        Directory.Delete(directory, recursive: true);
    }
}
