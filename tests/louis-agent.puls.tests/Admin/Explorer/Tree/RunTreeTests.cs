using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Explorer.Tree;

namespace LouisAgent.Puls.Tests.Admin.Explorer.Tree;

/// <summary>
/// One repeat of a stored run as the explorer's tree: the modes in the run's order, each conversation, turn and
/// request hung where it belongs, and what the components ask of the tree.
/// </summary>
public class RunTreeTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 10, 0, 0, TimeSpan.Zero);

    /// <summary>The modes come in the run's order, only the repeat asked for, with their toolsets.</summary>
    [Test]
    public void TheModes_AreTheRepeatsOwn_InTheRunsOrder()
    {
        var run = Run(modes: [Mode(SampleMode.Beta, 1), Mode(SampleMode.Alpha, 1), Mode(SampleMode.Gamma, 2)]);

        var tree = RunTree.Of(run, 1);

        Assert.That(tree.Modes.Select(m => m.Name), Is.EqualTo(new[] { SampleMode.Beta, SampleMode.Alpha }));
        Assert.That(tree.Modes[0].Toolsets, Is.EqualTo(new[] { SampleToolset.Alpha }));
    }

    /// <summary>
    /// A conversation or turn naming a mode the run's list didn't gets a mode of its own, and a turn whose
    /// conversation never came gets one.
    /// </summary>
    [Test]
    public void AModeOrConversationNobodyListed_IsMadeForIt()
    {
        var run = Run(modes: [], conversations: [Conversation(SampleMode.Alpha, "bobotie")],
            turns: [Turn(SampleMode.Beta, "curry", 1)]);

        var tree = RunTree.Of(run, 1);

        Assert.That(tree.Modes.Select(m => m.Name), Is.EqualTo(new[] { SampleMode.Alpha, SampleMode.Beta }));
        Assert.That(tree.Modes[1].Conversations.Single().Task, Is.EqualTo("curry"));
        Assert.That(tree.Modes[1].Conversations.Single().Turns.Single().N, Is.EqualTo(1));
    }

    /// <summary>Turns come in the order they happened, whatever order they were sent in.</summary>
    [Test]
    public void TheTurns_AreInTheirOrder()
    {
        var run = Run(modes: [Mode(SampleMode.Alpha, 1)], conversations: [Conversation(SampleMode.Alpha, "bobotie")],
            turns: [Turn(SampleMode.Alpha, "bobotie", 2), Turn(SampleMode.Alpha, "bobotie", 1)]);

        var turns = RunTree.Of(run, 1).Modes[0].Conversations[0].Turns;

        Assert.That(turns.Select(t => t.N), Is.EqualTo(new[] { 1, 2 }));
    }

    /// <summary>A request goes on its turn; with no turn, another repeat's or an unknown turn, it's left out.</summary>
    [Test]
    public void EachRequest_GoesOnItsTurn_AndOneWithNoTurnIsLeftOut()
    {
        var run = Run(modes: [Mode(SampleMode.Alpha, 1)], conversations: [Conversation(SampleMode.Alpha, "bobotie")],
            turns: [Turn(SampleMode.Alpha, "bobotie", 1)],
            requests:
            [
                Request(new TurnKeyRow(SampleMode.Alpha, 1, "bobotie", 1), 0.01m),
                Request(null, 0.02m),
                Request(new TurnKeyRow(SampleMode.Alpha, 2, "bobotie", 1), 0.03m),
                Request(new TurnKeyRow(SampleMode.Alpha, 1, "bobotie", 9), 0.04m)
            ]);

        var tree = RunTree.Of(run, 1);

        Assert.That(tree.Modes[0].Rows.Select(r => r.Cost), Is.EqualTo(new decimal?[] { 0.01m }));
    }

    /// <summary>The tasks once each, the first question asked of one, and the scales the bars share.</summary>
    [Test]
    public void TheTree_AnswersTheComponentsQuestions()
    {
        var run = Run(modes: [Mode(SampleMode.Alpha, 1), Mode(SampleMode.Beta, 1)],
            conversations:
            [
                Conversation(SampleMode.Alpha, "bobotie"), Conversation(SampleMode.Beta, "bobotie"),
                Conversation(SampleMode.Beta, "chat")
            ],
            turns: [Turn(SampleMode.Alpha, "bobotie", 1, question: ""), Turn(SampleMode.Beta, "bobotie", 1)],
            requests: [Request(new TurnKeyRow(SampleMode.Beta, 1, "bobotie", 1), 0.5m, tokens: 70)]);

        var tree = RunTree.Of(run, 1);

        Assert.That(tree.Tasks, Is.EqualTo(new[] { "bobotie", "chat" }));
        Assert.That(tree.Question("bobotie"), Is.EqualTo("What now?"));
        Assert.That(tree.Question("chat"), Is.Null);
        Assert.That(tree.TopCost, Is.EqualTo(0.5m));
        Assert.That(tree.TopTokens, Is.EqualTo(70));
    }

    /// <summary>A repeat that cost nothing still has a scale above zero, so no bar divides by it.</summary>
    [Test]
    public void AnEmptyRepeat_StillHasAScale()
    {
        var tree = RunTree.Of(Run(modes: [Mode(SampleMode.Alpha, 1)]), 1);

        Assert.That(tree.TopCost, Is.EqualTo(0.000001m));
        Assert.That(tree.TopTokens, Is.EqualTo(1));
    }

    /// <summary>A stored run with these modes, conversations, turns and requests.</summary>
    private static StoredRunReply Run(IReadOnlyList<ModeRow> modes,
        IReadOnlyList<ConversationRow>? conversations = null,
        IReadOnlyList<TurnRow>? turns = null, IReadOnlyList<RequestRow>? requests = null) =>
        new(new StoredRunRow("run1", "check", 2, "done", 0, T0, null, null, null, null, null, null, null, null, null),
            null, modes, null, [], null, conversations ?? [], [], turns ?? [], requests ?? []);

    /// <summary>A mode of a repeat, with one toolset.</summary>
    private static ModeRow Mode(string name, int repeat) =>
        new(name, repeat, [SampleToolset.Alpha], null, null, null, null, null, null, null);

    /// <summary>A conversation of the first repeat, with no grades yet.</summary>
    private static ConversationRow Conversation(string mode, string task) =>
        new(mode, 1, task, 0, null, 0, 1, null, null, []);

    /// <summary>A turn of the first repeat.</summary>
    private static TurnRow Turn(string mode, string task, int n, string question = "What now?") =>
        new(new TurnToolsRow(0, 0, []), mode, 1, task, n, "s1", question, "Bobotie.", null, null, 1, 0, [], 2, null);

    /// <summary>A request on a turn, with its cost and its input tokens.</summary>
    private static RequestRow Request(TurnKeyRow? turn, decimal cost, long tokens = 10) =>
        new(turn, T0, "turn", "s1", null, "claude-haiku", tokens, null, null, null, cost, 100);
}
