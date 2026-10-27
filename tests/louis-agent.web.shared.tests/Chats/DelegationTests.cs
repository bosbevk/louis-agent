using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Testing;
using LouisAgent.Web.Shared.Chats;

namespace LouisAgent.Web.Shared.Tests.Chats;

// T1: what a delegation card reads from its block (F5, F26, F30): the states it goes through, from the call that
// opened it to the footer the tool appends when it returns.
public class DelegationTests
{
    private static Block Call(string tool, string input, string? result = null) =>
        new() { Kind = BlockKind.Tool, ToolUseId = "call_1", ToolName = tool, ToolInput = input, ToolResult = result };

    [Test]
    public void AStart_StillRunning_ReadsTheCall()
    {
        var delegation = Delegation.From(Call("StartSubSession", SampleToolset.Fill("""{"agent":"puls","toolsets":["<Alpha>"],"message":"Kook iets","role":"puls-cook"}""")))!;

        Assert.That(delegation.Agent, Is.EqualTo("puls"));
        Assert.That(delegation.Role, Is.EqualTo("puls-cook"));
        Assert.That(delegation.Toolsets, Is.EqualTo(new[] { SampleToolset.Alpha }));
        Assert.That(delegation.Task, Is.EqualTo("Kook iets"));
        Assert.That(delegation.Answer, Is.Null);
        Assert.That(delegation.Failed, Is.False);
        Assert.That(delegation.Continued, Is.False);
    }

    [Test]
    public void AStart_ThatReturned_ReadsTheAnswerAndTheFooter()
    {
        var block = Call("StartSubSession", SampleToolset.Fill("""{"agent":"","toolsets":["<Alpha>"],"message":"Kook iets","role":"puls-cook"}"""),
            SampleToolset.Fill("Bobotie vir vier.\n\n[Sub-session sub-1 on puls (<Alpha>, <Beta>; puls-cook): 3 requests, $0.01. It stays open: ContinueSubSession]"));

        var delegation = Delegation.From(block)!;

        Assert.That(delegation.Answer, Is.EqualTo("Bobotie vir vier."));
        Assert.That(delegation.UsageLine, Is.EqualTo("3 requests, $0.01"));
        Assert.That(delegation.ResultAgent, Is.EqualTo("puls"), "the peer actually used, since the call named none");
        Assert.That(delegation.Handle, Is.EqualTo("sub-1"));
        Assert.That(delegation.Toolsets, Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }), "F34: what the specialist loaded wins over what was sent");
        Assert.That(delegation.Failed, Is.False);
    }

    [Test]
    public void AContinue_IsAFollowUp_ToTheHandle()
    {
        var delegation = Delegation.From(Call("ContinueSubSession", """{"handle":"sub-1","message":"Maak dit vir ses"}"""))!;

        Assert.That(delegation.Continued, Is.True);
        Assert.That(delegation.Handle, Is.EqualTo("sub-1"));
        Assert.That(delegation.Role, Is.Null, "the call names only the handle; the stream's start names the role");
        Assert.That(delegation.Task, Is.EqualTo("Maak dit vir ses"));
    }

    [Test]
    public void AContinue_LearnsItsRole_FromItsStreamsStart()
    {
        var block = Call("ContinueSubSession", """{"handle":"sub-1","message":"Meer"}""");
        block.SubTurn = new Turn { Role = "puls-cook", Toolsets = [SampleToolset.Alpha, SampleToolset.Beta] };

        var delegation = Delegation.From(block)!;

        Assert.That(delegation.Role, Is.EqualTo("puls-cook"), "FX17");
        Assert.That(delegation.Toolsets, Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }));
    }

    [Test]
    public void AFailedCall_IsFailed_WithTheErrorAsItsAnswer()
    {
        var delegation = Delegation.From(Call("StartSubSession", """{"agent":"puls","toolsets":["web"],"message":"Kook"}""", "Error: no peer named puls"))!;

        Assert.That(delegation.Failed, Is.True);
        Assert.That(delegation.Answer, Is.EqualTo("Error: no peer named puls"));
        Assert.That(delegation.UsageLine, Is.Null);
    }

    [Test]
    public void AMinion_ReadsItsRoleToolAndModel()
    {
        var block = Call("StartMinion", """{"role":"puls-minion-web-price-lookup","tool":"WebSearch","task":"Prys van rys"}""",
            "R24,99\n\n[Minion puls-minion-web-price-lookup on ollama-puls with WebSearch: 1 request, price unknown; handle sub-2: kept]");

        var delegation = Delegation.From(block)!;

        Assert.That(delegation.Minion, Is.True);
        Assert.That(delegation.Role, Is.EqualTo("puls-minion-web-price-lookup"));
        Assert.That(delegation.Toolsets, Is.EqualTo(new[] { "WebSearch" }));
        Assert.That(delegation.Llm, Is.EqualTo("ollama-puls"));
        Assert.That(delegation.Handle, Is.EqualTo("sub-2"));
        Assert.That(delegation.Answer, Is.EqualTo("R24,99"));
        Assert.That(delegation.UsageLine, Is.EqualTo("1 request, price unknown"));
    }

    [Test]
    public void UnreadableInput_StillGivesACard()
    {
        var delegation = Delegation.From(Call("StartSubSession", "not json"))!;

        Assert.That(delegation.Agent, Is.EqualTo(""));
        Assert.That(delegation.Task, Is.EqualTo(""));
        Assert.That(delegation.Toolsets, Is.Empty);
    }

    [Test]
    public void AnOrdinaryTool_IsNoDelegation()
    {
        Assert.That(Delegation.From(Call("WebSearch", """{"query":"x"}""")), Is.Null);
        Assert.That(Delegation.From(new Block { Kind = BlockKind.Text, Text = "StartSubSession" }), Is.Null);
        Assert.That(Delegation.IsDelegationTool("ContinueMinion"), Is.True);
        Assert.That(Delegation.IsDelegationTool("FetchUrl"), Is.False);
    }

    [Test]
    public async Task TheRecordedTurn_HandedTheMealToTheCook()
    {
        var turn = await SessionStreamFixture.ReplayAsync();
        var block = turn.Blocks.Single(b => b.ToolName == Delegation.ToolName);

        var delegation = Delegation.From(block)!;

        Assert.That(delegation.Role, Is.EqualTo("puls-cook"));
        Assert.That(delegation.Toolsets, Does.Contain(CoreToolset.Web.Name()), "the recorded turn is Puls's own");
        Assert.That(delegation.Answer, Is.Not.Null.And.Not.Empty);
        Assert.That(delegation.UsageLine, Does.Contain("request"));
        Assert.That(delegation.Failed, Is.False);
    }
}
