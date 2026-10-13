using LouisAgent.Core.Skills;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Config;
using LouisAgent.Core.Pricing;
using LouisAgent.Core.Usage;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Agent;

// FX14: a sub-session's specialist read 74 files and its prompt grew to 298k tokens, past the long-prompt price. Once
// the next request is expected to pass the budget, it keeps its tools with tool mode None, so the specialist answers.
public class SubSessionPromptBudgetTests
{
    private static AgentEngine Engine(IChatClient client, IUsageSink sink, int budget)
    {
        AgentOptions options = TestPaths.Agent();
        options.SubSessionPromptBudget = budget;
        return new(new MarkdownSkillProvider(Path.Combine(TestPaths.RepoRoot, "Skills", "HelloWorld.md")), client, options,
            toolsets: TestPaths.Toolsets(options, ["files"]), usageSink: sink);
    }

    /// <summary>Rounds 1–4 call a tool, each prompt 300 tokens bigger than the last; round 5 answers.</summary>
    private static FakeChatClient GrowingPrompt() => new(Enumerable.Range(1, 4)
        .Select<int, Func<IList<ChatMessage>, ChatResponse>>(i => _ => new ChatResponse(new ChatMessage(ChatRole.Assistant,
        [
            new FunctionCallContent($"call_{i}", "ExecuteSkill",
                new Dictionary<string, object?> { ["skillName"] = "SayHello", ["jsonArgs"] = "{\"name\":\"x\"}" }),
            new UsageContent(new UsageDetails { InputTokenCount = 300 * i, OutputTokenCount = 10 }),
        ])))
        .Append(_ => new ChatResponse(new ChatMessage(ChatRole.Assistant, "The answer, from what I found.")))
        .ToArray());

    private static async Task<FakeChatClient> RunAsync(int budget, string? parent)
    {
        var client = GrowingPrompt();
        var engine = Engine(client, new ListUsageSink(), budget);
        using (UsageScope.Begin(session: "sess_child", turn: 1, tags: new UsageTags(null, null, null, parent)))
            await foreach (var _ in engine.StreamPromptAsync(engine.NewHistory(), "count them")) { }
        return client;
    }

    [Test]
    public async Task ASubSession_AnswersWithoutTools_OnceTheNextPromptWouldPassTheBudget()
    {
        // Prompts 300, 600, 900: after the third, the next is expected at 1,200, over a budget of 1,000.
        var client = await RunAsync(budget: 1_000, parent: "sess_puls#1");

        Assert.That(client.Requests.Take(3).Select(r => r.Options?.ToolMode), Is.All.Not.EqualTo(ChatToolMode.None));
        Assert.That(client.Requests[3].Options?.ToolMode, Is.EqualTo(ChatToolMode.None));
        Assert.That(client.Requests[3].Options?.Tools, Is.Not.Empty, "the tool list stays the same (thinking is bound to it)");
    }

    [Test]
    public async Task TheUsersOwnSession_HasNoBudget()
    {
        var client = await RunAsync(budget: 1_000, parent: null);

        Assert.That(client.Requests.Select(r => r.Options?.ToolMode), Is.All.Not.EqualTo(ChatToolMode.None));
    }

    [Test]
    public async Task ABudgetOfZero_IsOff()
    {
        var client = await RunAsync(budget: 0, parent: "sess_puls#1");

        Assert.That(client.Requests.Select(r => r.Options?.ToolMode), Is.All.Not.EqualTo(ChatToolMode.None));
    }

    [Test]
    public void TheDefaultBudget_LeavesRoomUnderTheLongPromptPrice()
    {
        // The forced last request still carries the last round's tool results, so the budget sits below the price step
        // with room for a round (PR #28 review): at the step itself, that request would be priced at the long rate.
        var prices = PriceTable.Load(Path.Combine(TestPaths.RepoRoot, "config", "prices.json"));
        long step = prices.Find(new LlmOptions().Model)!.LongPrompt!.AboveTokens;

        Assert.That(AgentOptions.DefaultSubSessionPromptBudget, Is.LessThanOrEqualTo(step - 20_000));
    }

    [TestCase(null, AgentOptions.DefaultSubSessionPromptBudget)]
    [TestCase("60000", 60_000)]
    [TestCase("0", 0)]
    [TestCase("lots", AgentOptions.DefaultSubSessionPromptBudget)]
    public void TheBudget_ComesFromSubSessionPromptBudget(string? value, int expected)
    {
        var options = AgentOptions.FromEnvironment(name => name switch
        {
            "SUB_SESSION_PROMPT_BUDGET" => value,
            "WORKSPACE_ROOT" => TestPaths.RepoRoot,
            _ => null,
        });

        Assert.That(options.SubSessionPromptBudget, Is.EqualTo(expected));
    }
}
