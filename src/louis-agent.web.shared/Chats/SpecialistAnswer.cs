namespace LouisAgent.Web.Shared.Chats;

/// <summary>A specialist's sub-turn as its card shows it (F5; W2b, from DelegationCard): its work, then its answer.</summary>
public static class SpecialistAnswer
{
    /// <summary>
    /// The specialist's answer is its text after its last thinking or tool call; the notes it wrote between tool calls
    /// belong to its work.
    /// </summary>
    public static (List<Block> Work, string Answer) Split(Turn sub)
    {
        int last = sub.Blocks.FindLastIndex(b => b.Kind != BlockKind.Text);
        var work = sub.Blocks.Take(last + 1).ToList();
        string answer = string.Join("\n\n", sub.Blocks.Skip(last + 1).Select(b => b.Text).Where(t => t.Length > 0));
        return (work, answer);
    }
}
