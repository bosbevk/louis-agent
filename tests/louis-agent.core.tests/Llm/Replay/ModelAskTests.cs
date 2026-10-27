using LouisAgent.Core.Llm.Replay;
using LouisAgent.Core.Sessions.Replay.Store;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Tests.Llm.Replay;

// F18 step 4e.2: the key a recorded model answer is stored under. It is the recording format, so each part changes only on its own
// change, and a message's CreatedAt and MessageId never count: the tool loop stamps both on the messages the host appends, and the
// next turn's first request carries them (the 4e probe: a whole-message key missed on every turn after a tool call).
public class ModelAskTests
{
    private static ChatMessage[] Chat(string user = "hello") =>
        [new(ChatRole.System, "You are Puls."), new(ChatRole.User, user)];

    private static ModelAsk Ask(IEnumerable<ChatMessage>? messages = null, ChatOptions? options = null) =>
        ModelAsk.Of(messages ?? Chat(), options ?? new ChatOptions { ModelId = "m1" }, defaultModel: null);

    private static AIFunction Tool(string name, string description = "does it") =>
        AIFunctionFactory.Create(() => "x", name, description);

    [Test]
    public void MessagesThatDifferOnlyInCreatedAtAndMessageId_HaveTheSameKey()
    {
        var first = new ChatMessage(ChatRole.Tool, [new FunctionResultContent("c1", "ok")])
        { CreatedAt = DateTimeOffset.UnixEpoch, MessageId = "a" };
        var second = new ChatMessage(ChatRole.Tool, [new FunctionResultContent("c1", "ok")])
        { CreatedAt = DateTimeOffset.UtcNow, MessageId = "b" };

        Assert.That(Ask([.. Chat(), first]).ToAsk(), Is.EqualTo(Ask([.. Chat(), second]).ToAsk()));
    }

    [Test]
    public void AChangedText_ChangesTheMessagesPart_AndOnlyThat()
    {
        ModelAsk before = Ask(Chat("hello")), after = Ask(Chat("hullo"));

        Assert.That(after.Messages, Is.Not.EqualTo(before.Messages));
        Assert.That((after.Model, after.System, after.Tools, after.Settings),
            Is.EqualTo((before.Model, before.System, before.Tools, before.Settings)));
    }

    [Test]
    public void TheLeadingSystemMessages_AreInSystem_NotInMessages()
    {
        ModelAsk ask = Ask();

        Assert.That(ask.Messages, Has.Count.EqualTo(1));
        Assert.That(ask.Messages[0].Role, Is.EqualTo("user"));
        Assert.That(Ask([new ChatMessage(ChatRole.System, "You are Louis."), Chat()[1]]).System,
            Is.Not.EqualTo(ask.System));
    }

    [Test]
    public void ALaterSystemMessage_IsAMessage()
    {
        ModelAsk ask = Ask([.. Chat(), new ChatMessage(ChatRole.System, "note")]);

        Assert.That(ask.Messages.Select(m => m.Role), Is.EqualTo(new[] { "user", "system" }));
    }

    [Test]
    public void EachPart_ChangesOnItsOwnChange()
    {
        ModelAsk With(Action<ChatOptions> change)
        {
            var options = new ChatOptions
            { ModelId = "m1", Instructions = "i", Temperature = 0.2f, Tools = [Tool("A")] };
            change(options);
            return Ask(options: options);
        }

        ModelAsk baseline = With(_ => { });

        Assert.That(With(o => o.ModelId = "m2").Model, Is.Not.EqualTo(baseline.Model));
        Assert.That(With(o => o.Instructions = "j").System, Is.Not.EqualTo(baseline.System));
        Assert.That(With(o => o.Tools = [Tool("A", "other")]).Tools, Is.Not.EqualTo(baseline.Tools));
        Assert.That(With(o => o.Temperature = 0.9f).Settings, Is.Not.EqualTo(baseline.Settings));
        Assert.That(With(o => o.Temperature = 0.9f).Tools, Is.EqualTo(baseline.Tools),
            "settings leave the tools alone");
        Assert.That(With(o => o.Instructions = "j").Settings, Is.EqualTo(baseline.Settings),
            "the instructions are the system part, not the settings");
    }

    [Test]
    public void ToolsAreKeyedByNameAndHash_InTheOrderSent()
    {
        ModelAsk ask = Ask(options: new ChatOptions { Tools = [Tool("B"), Tool("A")] });

        Assert.That(ask.Tools.Select(t => t.Name), Is.EqualTo(new[] { "B", "A" }));
        Assert.That(ask.Tools[0].Hash, Does.Match("^[0-9a-f]{64}$"));
    }

    [Test]
    public void WithNoModelInTheOptions_TheClientsDefaultIsTheModel()
    {
        Assert.That(ModelAsk.Of(Chat(), new ChatOptions(), "default-model").Model, Is.EqualTo("default-model"));
        Assert.That(ModelAsk.Of(Chat(), new ChatOptions { ModelId = "m1" }, "default-model").Model, Is.EqualTo("m1"));
    }

    [Test]
    public void TheKey_IsKindModel_WithSnakeCaseParts_AndReadsBack()
    {
        ReplayAsk ask = Ask(options: new ChatOptions { ModelId = "m1", Tools = [Tool("A")] }).ToAsk();

        Assert.That(ask.Kind, Is.EqualTo(ReplayKind.Model));
        Assert.That(ask.Key, Does.StartWith("{\"model\":\"m1\",\"system\":\"").And
            .Contain("\"tools\":[{\"name\":\"A\",\"hash\":\"")
            .And.Contain("\"settings\":\"").And.Contain("\"messages\":[{\"role\":\"user\",\"hash\":\""));
        Assert.That(ModelAsk.From(ask)!.ToAsk(), Is.EqualTo(ask));
    }

    [Test]
    public void AnAskOfAnotherKind_DoesNotReadBackAsAModelAsk()
    {
        Assert.That(ModelAsk.From(ReplayAsk.Of(ReplayKind.Search, new { Query = "q" })), Is.Null);
    }
}
