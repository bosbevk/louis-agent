using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Web.Shared.Chats;

namespace LouisAgent.Web.Shared.Tests.Chats;

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

    /// <summary>While a start still runs, the card shows who it went to, the role, the toolsets and the task.</summary>
    [Test]
    public void AStart_StillRunning_ReadsTheCall()
    {
        var delegation = Delegation.From(Call("StartSubSession",
                SampleToolset.Fill(SampleRole.Fill(SampleIdentity.Fill(
                    """{"agent":"<IdentityAlpha>","toolsets":["<Alpha>"],"message":"Kook iets","role":"<RoleAlpha>"}""")))))
            !;

        Assert.That(delegation.Agent, Is.EqualTo(SampleIdentity.Alpha));
        Assert.That(delegation.Role, Is.EqualTo(SampleRole.Alpha));
        Assert.That(delegation.Toolsets, Is.EqualTo(new[] { SampleToolset.Alpha }));
        Assert.That(delegation.Task, Is.EqualTo("Kook iets"));
        Assert.That(delegation.Answer, Is.Null);
        Assert.That(delegation.Failed, Is.False);
        Assert.That(delegation.Continued, Is.False);
    }

    /// <summary>
    /// Once it returned, the answer is split from the footer, and the footer says which peer it was, its handle and
    /// what it loaded.
    /// </summary>
    [Test]
    public void AStart_ThatReturned_ReadsTheAnswerAndTheFooter()
    {
        var block = Call("StartSubSession",
            SampleToolset.Fill(
                SampleRole.Fill("""{"agent":"","toolsets":["<Alpha>"],"message":"Kook iets","role":"<RoleAlpha>"}""")),
            SampleToolset.Fill(SampleRole.Fill(SampleIdentity.Fill(
                "Bobotie vir vier.\n\n[Sub-session sub-1 on <IdentityAlpha> (<Alpha>, <Beta>; <RoleAlpha>). " +
                "It stays open: ContinueSubSession]"))));

        var delegation = Delegation.From(block)!;

        Assert.That(delegation.Answer, Is.EqualTo("Bobotie vir vier."));
        Assert.That(delegation.ResultAgent, Is.EqualTo(SampleIdentity.Alpha),
            "the peer actually used, since the call named none");
        Assert.That(delegation.Handle, Is.EqualTo("sub-1"));
        Assert.That(delegation.Toolsets, Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }),
            "F34: what the specialist loaded wins over what was sent");
        Assert.That(delegation.Failed, Is.False);
    }

    /// <summary>A start that isn't kept has the short footer, with no handle; its answer is still split off.</summary>
    [Test]
    public void AStart_ThatWasClosed_ReadsTheShortFooter()
    {
        var block = Call("StartSubSession",
            SampleToolset.Fill("""{"agent":"coder","toolsets":["<Alpha>"],"message":"Fix it"}"""),
            SampleToolset.Fill("Fixed.\n\n[Sub-session on coder (<Alpha>, <Beta>)]"));

        var delegation = Delegation.From(block)!;

        Assert.That((delegation.Answer, delegation.ResultAgent, delegation.Handle), Is.EqualTo(("Fixed.", "coder",
            (string?)null)));
        Assert.That(delegation.Toolsets, Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }));
    }

    /// <summary>A continue is a follow-up to the handle it names; its role comes later, from its stream.</summary>
    [Test]
    public void AContinue_IsAFollowUp_ToTheHandle()
    {
        var delegation =
            Delegation.From(Call("ContinueSubSession", """{"handle":"sub-1","message":"Maak dit vir ses"}"""))!;

        Assert.That(delegation.Continued, Is.True);
        Assert.That(delegation.Handle, Is.EqualTo("sub-1"));
        Assert.That(delegation.Role, Is.Null, "the call names only the handle; the stream's start names the role");
        Assert.That(delegation.Task, Is.EqualTo("Maak dit vir ses"));
    }

    /// <summary>A continue learns its role and toolsets from the start of its own stream (FX17).</summary>
    [Test]
    public void AContinue_LearnsItsRole_FromItsStreamsStart()
    {
        var block = Call("ContinueSubSession", """{"handle":"sub-1","message":"Meer"}""");
        block.SubTurn = new Turn
        {
            Role = SampleRole.Alpha,
            Toolsets = [SampleToolset.Alpha, SampleToolset.Beta]
        };

        var delegation = Delegation.From(block)!;

        Assert.That(delegation.Role, Is.EqualTo(SampleRole.Alpha), "FX17");
        Assert.That(delegation.Toolsets, Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Beta }));
    }

    /// <summary>A call that failed shows its error as the answer.</summary>
    [Test]
    public void AFailedCall_IsFailed_WithTheErrorAsItsAnswer()
    {
        var delegation = Delegation.From(Call("StartSubSession",
            SampleIdentity.Fill("""{"agent":"<IdentityAlpha>","toolsets":["web"],"message":"Kook"}"""),
            SampleIdentity.Fill("Error: no peer named <IdentityAlpha>")))!;

        Assert.That(delegation.Failed, Is.True);
        Assert.That(delegation.Answer, Is.EqualTo(SampleIdentity.Fill("Error: no peer named <IdentityAlpha>")));
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
        var delegation = Delegation.From(Call("StartSubSession", "not json"))!;

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
            Text = "StartSubSession"
        }), Is.Null);
        Assert.That(Delegation.IsDelegationTool("ContinueMinion"), Is.True);
        Assert.That(Delegation.IsDelegationTool("FetchUrl"), Is.False);
    }

    /// <summary>
    /// The recorded Puls turn handed the meal to the cook: its card has the role, the toolsets and the answer, and what
    /// the cook cost comes from the cook's own events, not from the footer (FX27).
    /// </summary>
    [Test]
    public async Task TheRecordedTurn_HandedTheMealToTheCook()
    {
        var turn = await SessionStreamFixture.ReplayAsync();
        var block = turn.Blocks.Single(b => b.ToolName == Delegation.ToolName);

        var delegation = Delegation.From(block)!;

        Assert.That(delegation.Role, Is.EqualTo(SessionStreamFixture.CookRole));
        Assert.That(delegation.Toolsets, Does.Contain(CoreToolset.Web.Wire()), "the recorded turn is Puls's own");
        Assert.That(delegation.Answer, Is.Not.Null.And.Not.Empty.And.Not.Contain("[Sub-session"));
        Assert.That(block.SubTurn?.Usage?.Requests, Is.GreaterThan(0), "the cook's own events carry what it used");
        Assert.That(delegation.Failed, Is.False);
    }
}
