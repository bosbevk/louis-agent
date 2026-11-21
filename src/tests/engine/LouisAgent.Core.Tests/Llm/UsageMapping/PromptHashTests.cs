using LouisAgent.Core.Llm.UsageMapping;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Llm.UsageMapping;

// F18 step 4c: the prompt's version is the hash of what defines it, so the same prompt and tools always hash the same.
public class PromptHashTests
{
    private static readonly JsonElement EmptySchema =
        JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement;

    private static AITool Tool(string name = "GetToday", string description = "Today's date.",
        JsonElement? schema = null) =>
        AIFunctionFactory.CreateDeclaration(name, description, schema ?? EmptySchema);

    private static List<ChatMessage> Prompt(string system = "Jy is Puls. Hê, it's here.") =>
        [new(ChatRole.System, system), new(ChatRole.User, "hi")];

    private static string Hash(IEnumerable<ChatMessage> messages, params AITool[] tools) =>
        PromptHash.Of(messages, new ChatOptions { Tools = tools });

    [Test]
    public void TheEncoding_IsPinned()
    {
        // A changed encoding changes every hash, and the runs recorded before the change stop comparing with the new ones.
        const string json =
            """{"system":["Jy is Puls. Hê, it's here."],"instructions":null,"tools":[{"name":"GetToday","description":"Today's date.","schema":{"type":"object","properties":{}}}]}""";
        string expected = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json)));

        Assert.That(Hash(Prompt(), Tool()), Is.EqualTo(expected));
    }

    [Test]
    public void TheConversationAfterThePrompt_DoesNotCount()
    {
        List<ChatMessage> longer = Prompt();
        longer.Add(new ChatMessage(ChatRole.User, "and another"));
        longer.Add(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent("c1", "GetToday")]));
        longer.Add(new ChatMessage(ChatRole.Tool, [new FunctionResultContent("c1", "2026-10-25")]));
        longer.Add(new ChatMessage(ChatRole.System, "A system message that comes later."));

        Assert.That(Hash(longer, Tool()), Is.EqualTo(Hash(Prompt(), Tool())));
    }

    private static IEnumerable<TestCaseData> Changes()
    {
        yield return new TestCaseData(Prompt("Jy is Puls. Hê, it's there."), new[] { Tool() }, null).SetName(
            "a line of the system prompt");
        yield return new TestCaseData(
            new List<ChatMessage>
            {
                new(ChatRole.System, "Jy is Puls. Hê, it's here."), new(ChatRole.System, "No tools."),
                new(ChatRole.User, "hi")
            },
            new[] { Tool() }, null).SetName("a second leading system message");
        yield return new TestCaseData(Prompt(), new[] { Tool() }, "Be brief.").SetName("instructions");
        yield return new TestCaseData(Prompt(), new[] { Tool(description: "Today's date and time.") }, null).SetName(
            "a tool's description");
        yield return new TestCaseData(
                Prompt(),
                new[]
                {
                    Tool(schema: JsonDocument.Parse("""{"type":"object","properties":{"zone":{"type":"string"}}}""")
                        .RootElement)
                }, null)
            .SetName("a parameter added to a schema");
        yield return new TestCaseData(Prompt(), new[] { Tool("Other"), Tool() }, null).SetName("a tool added");
        yield return new TestCaseData(Prompt(), Array.Empty<AITool>(), null).SetName("a tool left out");
    }

    [TestCaseSource(nameof(Changes))]
    public void EachOfThese_ChangesTheHash(List<ChatMessage> messages, AITool[] tools, string? instructions)
    {
        string before = Hash(Prompt(), Tool());

        Assert.That(PromptHash.Of(messages, new ChatOptions { Tools = tools, Instructions = instructions }),
            Is.Not.EqualTo(before));
    }

    [Test]
    public void TwoToolsSwapped_ChangeTheHash()
    {
        Assert.That(Hash(Prompt(), Tool("A"), Tool("B")), Is.Not.EqualTo(Hash(Prompt(), Tool("B"), Tool("A"))));
    }

    [Test]
    public void NoOptionsAndOnlyAUserMessage_StillGiveAHash()
    {
        string hash = PromptHash.Of([new ChatMessage(ChatRole.User, "hi")], null);

        Assert.That(hash, Does.Match("^[0-9a-f]{64}$"));
    }
}
