using LouisAgent.Web.Chats;

namespace LouisAgent.Web.Tests.Chats;

/// <summary>
/// What a delegation card reads from its block (F5, F26, F30): the states it goes through, from the call that opened
/// it to the footer the tool appends when it returns.
/// </summary>
public class DelegationTests
{
    /// <summary>A tool block with the call's input and, once it returned, its result.</summary>
    private static Block Call(string tool, string input, string? result = null) =>
        new()
        {
            Kind = BlockKind.Tool,
            ToolUseId = "call_1",
            ToolName = tool,
            ToolInput = input,
            ToolResult = result
        };

    /// <summary>
    /// An ask learns what its child loaded from the start of the child's own stream (F34), since the call sends none.
    /// </summary>
    [Test]
    public void AnAsk_LearnsItsToolsets_FromItsStreamsStart()
    {
        var block = Call(Delegation.AskToolName, """{"address":"acme.cook","message":"Meer"}""");
        block.SubTurn = new Turn { Toolsets = [SampleToolset.Alpha, SampleToolset.Beta] };

        Assert.That(Delegation.From(block)!.Toolsets, Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }));
    }

    /// <summary>
    /// A minion's card reads its role and tool from the call, and the model it ran on from the footer.
    /// </summary>
    [Test]
    public void AMinion_ReadsItsRoleToolAndModel()
    {
        var block = Call("StartMinion",
            SampleRole.Fill("""{"role":"<RoleZeta>","tool":"WebSearch","task":"Prys van rys"}"""),
            SampleRole.Fill("R24,99\n\n[Minion <RoleZeta> on ollama-puls with WebSearch]"));

        var delegation = Delegation.From(block)!;

        Assert.That(delegation.Minion, Is.True);
        Assert.That(delegation.Role, Is.EqualTo(SampleRole.Zeta));
        Assert.That(delegation.Toolsets, Is.EqualTo(new[] { "WebSearch" }));
        Assert.That(delegation.Llm, Is.EqualTo("ollama-puls"));
        Assert.That(delegation.Answer, Is.EqualTo("R24,99"));
    }

    /// <summary>Input that isn't JSON still gives a card, with what can be shown.</summary>
    [Test]
    public void UnreadableInput_StillGivesACard()
    {
        var delegation = Delegation.From(Call(Delegation.AskToolName, "not json"))!;

        Assert.That(delegation.Agent, Is.EqualTo(""));
        Assert.That(delegation.Task, Is.EqualTo(""));
        Assert.That(delegation.Toolsets, Is.Empty);
    }

    /// <summary>Any other tool, or a text block, is no delegation.</summary>
    [Test]
    public void AnOrdinaryTool_IsNoDelegation()
    {
        Assert.That(Delegation.From(Call("WebSearch", """{"query":"x"}""")), Is.Null);
        Assert.That(Delegation.From(new Block
        {
            Kind = BlockKind.Text,
            Text = "AskAgent"
        }), Is.Null);
        Assert.That(Delegation.IsDelegationTool("ContinueMinion"), Is.True);
        Assert.That(Delegation.IsDelegationTool("FetchUrl"), Is.False);
    }

    /// <summary>
    /// An ask (F127.3.1) is a delegation too: while it runs, the card shows the address it went to and the message.
    /// </summary>
    [Test]
    public void AnAsk_StillRunning_ReadsTheAddressAndTheMessage()
    {
        var delegation = Delegation.From(Call(Delegation.AskToolName,
            """{"address":"seemeeu-guesthouse","message":"Is a dog allowed?"}"""));

        Assert.That(delegation, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(Delegation.IsDelegationTool(Delegation.AskToolName), Is.True);
            Assert.That(delegation!.Agent, Is.EqualTo("seemeeu-guesthouse"));
            Assert.That(delegation.Task, Is.EqualTo("Is a dog allowed?"));
            Assert.That((delegation.Answer, delegation.Failed), Is.EqualTo(((string?)null, false)));
        });
    }

    /// <summary>
    /// Once it returned, the answer is split from the line under it, which names who answered, whether the
    /// conversation was new or not.
    /// </summary>
    [TestCase("[Answered by seemeeu-guesthouse, did: SendLead]")]
    [TestCase("[Answered by seemeeu-guesthouse, did: no tools. A new conversation: the earlier one had closed, so " +
              "it remembers none of it]")]
    public void AnAsk_ThatReturned_ReadsTheAnswerAndWhoGaveIt(string line)
    {
        var delegation = Delegation.From(Call(Delegation.AskToolName,
            """{"address":"seemeeu-guesthouse","message":"Book it."}""", $"Booked for Friday.\n\n{line}"))!;

        Assert.Multiple(() =>
        {
            Assert.That(delegation.Answer, Is.EqualTo("Booked for Friday."));
            Assert.That(delegation.ResultAgent, Is.EqualTo("seemeeu-guesthouse"));
            Assert.That(delegation.Failed, Is.False);
        });
    }

    /// <summary>An ask that was refused shows the refusal as its answer, marked failed.</summary>
    [Test]
    public void ARefusedAsk_ShowsTheRefusal()
    {
        const string refusal = "Error: guide may not call acme. It may call: seemeeu-guesthouse.";

        var delegation = Delegation.From(Call(Delegation.AskToolName,
            """{"address":"acme","message":"Hi"}""", refusal))!;

        Assert.That((delegation.Answer, delegation.Failed), Is.EqualTo((refusal, true)));
    }

    /// <summary>
    /// The recorded guide turn asked Seemeeu: its card names the address and no role (a voice's role is its mode's
    /// path), holds Seemeeu's own answer without the line under it, and what Seemeeu loaded, from its own stream.
    /// </summary>
    [Test]
    public async Task TheRecordedAsk_ReadsSeemeeusAnswer()
    {
        var turn = await SessionStreamFixture.ReplayAskAsync();
        var block = turn.Blocks.Single(b => b.ToolName == Delegation.AskToolName);

        var delegation = Delegation.From(block)!;

        Assert.Multiple(() =>
        {
            Assert.That((delegation.Agent, delegation.ResultAgent),
                Is.EqualTo(("seemeeu-guesthouse", "seemeeu-guesthouse")));
            Assert.That(delegation.Answer, Does.Contain("garden room").And.Not.Contain("[Answered by"));
            Assert.That(delegation.Toolsets, Is.EqualTo(new[] { "leads", "site" }), "Seemeeu's own packs");
            Assert.That(delegation.Role, Is.Null);
            Assert.That(delegation.Failed, Is.False);
        });
    }

    /// <summary>
    /// The recorded Puls turn asked the cook: its card has the address and the answer, and what the cook cost comes
    /// from the cook's own events, not from the line under its answer (FX27). It names no toolsets: it was recorded
    /// before a child's stream named what it loaded, and an ask's line names none.
    /// </summary>
    [Test]
    public async Task TheRecordedTurn_AskedTheCook()
    {
        var turn = await SessionStreamFixture.ReplayAsync();
        var block = turn.Blocks.Single(b => b.ToolName == Delegation.AskToolName);

        var delegation = Delegation.From(block)!;

        Assert.Multiple(() =>
        {
            Assert.That(delegation.Agent, Is.EqualTo(SessionStreamFixture.CookAddress));
            Assert.That(delegation.Answer, Is.Not.Null.And.Not.Empty.And.Not.Contain("[Answered by"));
            Assert.That(block.SubTurn?.Usage?.Requests, Is.GreaterThan(0), "the cook's own events carry what it used");
            Assert.That(delegation.Failed, Is.False);
        });
    }
}
