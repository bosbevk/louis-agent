using Anthropic.Models.Messages;
using LouisAgent.Core.Anthropic;
using LouisAgent.Core.Config;
using LouisAgent.Core.Llm;
using LouisAgent.Core.Llm.Caching;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.IntegrationTests.Llm.ToolSearch;

/// <summary>
/// The proof tool search stands on (F97.3 step 1): Haiku 5.5, built as a host builds it, with thinking on and the cache
/// marker our caching adds, is sent twenty tools with eighteen of them deferred. A search tool of our own answers with
/// Anthropic's tool references; the model then calls a tool it could not see, the request after it is accepted with
/// the thinking kept, and the second message reads the cache. A few live calls on a small model, a few US cents.
/// </summary>
/// <remarks>
/// There is no code of ours behind it to see fail first: it proves Anthropic's API and the SDK's adapter, which no
/// page says carry thinking and the cache through a found tool. If it fails, F97.3's plan stops here.
/// </remarks>
[Category("Integration")]
public class ToolSearchProofTests
{
    /// <summary>The small model the proof runs on.</summary>
    private const string Model = "claude-haiku-5-5";

    /// <summary>The key the SDK's adapter reads a tool's <c>defer_loading</c> from.</summary>
    private const string DeferLoading = "DeferLoading";

    /// <summary>The tool the model can't see at first, and must find.</summary>
    private const string Hidden = "ShowCommit";

    /// <summary>What the proof's commit says, so the answer can be checked against it.</summary>
    private const string CommitMessage = "fix: the basket total rounds to cents";

    /// <summary>The calls in the order they ran, so the proof sees a search come before the hidden tool.</summary>
    private readonly List<string> _calls = [];

    /// <summary>
    /// The model finds the hidden tool with our search, calls it by its own name, answers from it, and a second
    /// message in the same chat is accepted and reads the cache.
    /// </summary>
    [Test]
    public async Task ADeferredTool_IsFoundCalledAndAnsweredFrom_WithThinkingAndTheCache()
    {
        string key = Outside.Key("ANTHROPIC_API_KEY", "the tool search proof");
        var options = new LlmOptions
        {
            Provider = LlmOptions.Anthropic,
            Model = Model,
            ApiKey = key
        };
        var provider = AnthropicProvider.Instance;

        // The model as a host builds it: the provider's client, our cache marker, and the tool loop.
        var model = new LlmClientFactory([provider]).Create(options);
        var cached = new PromptCachingChatClient(model, provider.CreatePromptCaching(options));
        var chat = new ChatClientBuilder(cached).UseFunctionInvocation().Build();
        var request = new ChatOptions
        {
            Tools = Tools(),
            Reasoning = new ReasoningOptions
            {
                Effort = ReasoningEffort.Medium,
                Output = ReasoningOutput.Full
            }
        };

        // The first message needs the hidden tool, the second only the history.
        List<ChatMessage> history =
        [
            new(ChatRole.System, "You work in a git repository. Most of your tools are not listed until you look "
                                 + "for them: FindTool finds a git, file or build tool by what it does."),
            new(ChatRole.User, "What does the last commit say?")
        ];
        var first = await chat.GetResponseAsync(history, request);
        history.AddRange(first.Messages);
        history.Add(new ChatMessage(ChatRole.User, "Say it again in five words."));
        var second = await chat.GetResponseAsync(history, request);

        TestContext.Out.WriteLine($"calls: {string.Join(", ", _calls)}");
        TestContext.Out.WriteLine($"first: in {first.Usage?.InputTokenCount}, out {first.Usage?.OutputTokenCount}");
        TestContext.Out.WriteLine(
            $"second: in {second.Usage?.InputTokenCount}, cached {second.Usage?.CachedInputTokenCount}");
        Assert.Multiple(() =>
        {
            // A deferred tool can't be called before it is found: a call to it first would mean it was never deferred.
            Assert.That(_calls, Does.Contain(Hidden), "the hidden tool was called");
            Assert.That(_calls.IndexOf("FindTool"), Is.InRange(0, _calls.IndexOf(Hidden)),
                "the search came before it");
            Assert.That(first.Text, Does.Contain("cents"), "the answer came from the hidden tool");
            Assert.That(first.Messages.SelectMany(m => m.Contents).OfType<TextReasoningContent>(), Is.Not.Empty,
                "the model thought");
            Assert.That(second.Usage?.CachedInputTokenCount, Is.GreaterThan(0), "the second message read the cache");
        });
    }

    /// <summary>
    /// The twenty tools: our search and the clock loaded, and eighteen git and file tools deferred, the hidden one
    /// among them.
    /// </summary>
    private List<AITool> Tools()
    {
        List<AITool> tools =
        [
            AIFunctionFactory.Create(FindTool, "FindTool",
                "Finds the tools that do what you describe, among the ones not listed yet, and loads them."),
            AIFunctionFactory.Create(() => "2026-11-27", "GetToday", "Today's date.")
        ];
        tools.Add(Deferred(() => Ran(Hidden, CommitMessage), Hidden,
            "Show the last commit, or one by its id: its message, author and changes (git show)."));
        foreach (var (name, does) in Others)
            tools.Add(Deferred(() => Ran(name, "nothing to see"), name, does));
        return tools;
    }

    /// <summary>Notes that a tool ran, and gives back its answer.</summary>
    private string Ran(string tool, string answer)
    {
        _calls.Add(tool);
        return answer;
    }

    /// <summary>
    /// Our search, answered the way Anthropic's custom tool search asks: a tool reference per tool found, which the
    /// API expands into that tool's definition. The proof always finds the hidden tool and one other.
    /// </summary>
    private IEnumerable<AIContent> FindTool(string query)
    {
        _calls.Add("FindTool");
        return [Reference(Hidden), Reference("GetLog")];
    }

    /// <summary>A tool reference block, carried as the raw form the SDK's adapter passes through.</summary>
    private static AIContent Reference(string tool) =>
        new() { RawRepresentation = new Block(new ToolReferenceBlockParam { ToolName = tool }) };

    /// <summary>A tool sent with <c>defer_loading</c>, through the key the SDK's adapter reads.</summary>
    private static AIFunction Deferred(Delegate method, string name, string description) =>
        AIFunctionFactory.Create(method, new AIFunctionFactoryOptions
        {
            Name = name,
            Description = description,
            AdditionalProperties = new Dictionary<string, object?> { [DeferLoading] = true }
        });

    /// <summary>The other seventeen deferred tools, by name and what each does.</summary>
    private static readonly (string Name, string Does)[] Others =
    [
        ("GetLog", "List the recent commits, newest first (git log)."),
        ("GetStatus", "Show which files are changed, staged or new (git status)."),
        ("Stage", "Stage files for the next commit (git add)."),
        ("Unstage", "Take files out of the next commit (git restore --staged)."),
        ("Commit", "Commit what is staged, with a message (git commit)."),
        ("CreateBranch", "Create a new branch (git branch)."),
        ("SwitchBranch", "Switch to another branch (git switch)."),
        ("ListBranches", "List the branches (git branch --list)."),
        ("Merge", "Merge a branch into the current one (git merge)."),
        ("Push", "Push the current branch to its remote (git push)."),
        ("Pull", "Pull the current branch from its remote (git pull)."),
        ("DiffUnstaged", "Show the changes not yet staged (git diff)."),
        ("ReadWorkspaceFile", "Read a file in the workspace."),
        ("WriteWorkspaceFile", "Write a file in the workspace."),
        ("FindFiles", "Find files in the workspace by a name pattern."),
        ("SearchWorkspace", "Search the workspace's files for a text."),
        ("DotNetBuild", "Build a .NET project or solution (dotnet build).")
    ];
}
