using LouisAgent.Api.Contracts.Admin.Runs;

namespace LouisAgent.Puls.Admin.Explorer.Tree;

/// <summary>
/// One repeat of a stored run as the explorer drills into it: its modes side by side, each with its conversations,
/// their turns and the model requests each turn made.
/// </summary>
/// <remarks>
/// Built once per repeat from the contract's <c>StoredRunReply</c>, so no component picks the run's JSON apart; the
/// components ask the tree (the tasks, a question, the dearest conversation) instead of walking it themselves.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed class RunTree
{
    /// <summary>A tree with nothing in it, for before the run is read.</summary>
    public static readonly RunTree Empty = new([]);

    /// <summary>Hands the tree its modes, already built.</summary>
    private RunTree(IReadOnlyList<ModeNode> modes) => Modes = modes;

    /// <summary>The modes, in the run's order.</summary>
    public IReadOnlyList<ModeNode> Modes { get; }

    /// <summary>The modes side by side, each with its colour number.</summary>
    public IEnumerable<(ModeNode Mode, int I)> Columns => Modes.Select((m, i) => (m, i));

    /// <summary>Every task in the repeat, once each, in the order the modes have them.</summary>
    public List<string> Tasks => Modes.SelectMany(m => m.Conversations.Select(c => c.Task)).Distinct().ToList();

    /// <summary>The dearest conversation's cost, so every bar is drawn on one scale; never zero.</summary>
    public decimal TopCost =>
        Math.Max(0.000001m, Conversations.Select(c => c.Cost ?? 0m).DefaultIfEmpty(0m).Max());

    /// <summary>The most tokens a conversation used, so every stack is drawn on one scale; never zero.</summary>
    public long TopTokens =>
        Math.Max(1, Conversations.Select(c => c.Rows.Sum(r => r.Tokens)).DefaultIfEmpty(0L).Max());

    /// <summary>Every mode's conversations, one after the other.</summary>
    private IEnumerable<ConversationNode> Conversations => Modes.SelectMany(m => m.Conversations);

    /// <summary>Each task with its turns' numbers, across the modes: what another repeat is checked against.</summary>
    public IEnumerable<(string Task, IEnumerable<int> Turns)> Shape =>
        Conversations.Select(c => (c.Task, c.Turns.Select(t => t.N)));

    /// <summary>The first question asked for a task, from whichever mode has one.</summary>
    public string? Question(string? task) =>
        Conversations.Where(c => c.Task == task).SelectMany(c => c.Turns).Select(t => t.Question)
            .FirstOrDefault(q => q.Length > 0);

    /// <summary>
    /// Builds the tree of one repeat: the modes in the run's order, then the conversations, turns and requests.
    /// </summary>
    /// <remarks>
    /// Each pass hangs its rows on what the pass before made: a conversation or turn naming a mode the run's list
    /// didn't gets one, a turn whose conversation never came gets one, and a request with no turn is left out.
    /// </remarks>
    public static RunTree Of(StoredRunReply run, int repeat)
    {
        // One node per mode of this repeat, in the run's order.
        var modes = (run.Modes ?? []).Where(m => m.Repeat == repeat)
            .Select(m => new ModeNode
            {
                Name = m.Name,
                Toolsets = m.Toolsets ?? []
            }).ToList();

        // Each conversation on its mode, with its scores and grades.
        foreach (var c in (run.Conversations ?? []).Where(c => c.Repeat == repeat))
            ModeOf(modes, c.Mode).Conversations.Add(ConversationNode.Of(c));

        // Each turn on its conversation, with the tool counts the API worked out.
        foreach (var t in (run.Turns ?? []).Where(t => t.Repeat == repeat))
            ConversationOf(ModeOf(modes, t.Mode), t.Task).Turns.Add(TurnNode.Of(t));

        // Each ledger request on the turn it belongs to.
        foreach (var r in run.Requests ?? [])
            TurnOf(modes, r.Turn, repeat)?.Rows.Add(RequestNode.Of(r));

        // Turns in the order they happened.
        foreach (var c in modes.SelectMany(m => m.Conversations)) c.Turns.Sort((a, b) => a.N.CompareTo(b.N));
        return new RunTree(modes);
    }

    /// <summary>Finds a mode by name, making one if the run's list didn't name it.</summary>
    private static ModeNode ModeOf(List<ModeNode> modes, string name)
    {
        var mode = modes.FirstOrDefault(m => m.Name == name);
        if (mode is not null) return mode;

        mode = new ModeNode { Name = name };
        modes.Add(mode);
        return mode;
    }

    /// <summary>Finds a mode's conversation of a task, making one if its line hasn't come.</summary>
    private static ConversationNode ConversationOf(ModeNode mode, string task)
    {
        var conversation = mode.Conversation(task);
        if (conversation is not null) return conversation;

        conversation = new ConversationNode { Task = task };
        mode.Conversations.Add(conversation);
        return conversation;
    }

    /// <summary>The turn a request names in this repeat; none when it names no turn, or one the tree hasn't.</summary>
    private static TurnNode? TurnOf(List<ModeNode> modes, TurnKeyRow? key, int repeat)
    {
        if (key is null || key.Repeat != repeat) return null;
        return modes.FirstOrDefault(m => m.Name == key.Mode)?.Conversation(key.Task)?.Turns
            .FirstOrDefault(t => t.N == key.Turn);
    }
}
