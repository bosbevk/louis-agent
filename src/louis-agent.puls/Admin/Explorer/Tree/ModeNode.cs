using LouisAgent.Web.Shared.Usage;

namespace LouisAgent.Puls.Admin.Explorer.Tree;

/// <summary>
/// One mode of the run: its name, its toolsets and its conversations, with the totals the explorer shows for it.
/// </summary>
public sealed class ModeNode
{
    /// <summary>The mode's name.</summary>
    public string Name { get; init; } = "";

    /// <summary>The toolsets its sessions opened with.</summary>
    public IReadOnlyList<string> Toolsets { get; init; } = [];

    /// <summary>Its conversations, in the order the run sent them.</summary>
    public List<ConversationNode> Conversations { get; } = [];

    /// <summary>All its conversations' requests.</summary>
    public List<RequestNode> Rows => Conversations.SelectMany(c => c.Rows).ToList();

    /// <summary>All its conversations' turns.</summary>
    public List<TurnNode> Turns => Conversations.SelectMany(c => c.Turns).ToList();

    /// <summary>What its requests cost; nothing if none had a known price, never zero.</summary>
    public decimal? Cost => UsageFormat.KnownSum(Rows.Select(r => r.Cost));

    /// <summary>All its requests' tokens added up.</summary>
    public long Tokens => Rows.Sum(r => r.Tokens);

    /// <summary>All its turns' time added up.</summary>
    public double Seconds => Turns.Sum(t => t.Seconds);

    /// <summary>How many errors its conversations had in all.</summary>
    public int Errors => Conversations.Sum(c => c.Errors);

    /// <summary>Its conversation of a task, if it has one.</summary>
    public ConversationNode? Conversation(string? task) =>
        task is null ? null : Conversations.FirstOrDefault(c => c.Task == task);
}
