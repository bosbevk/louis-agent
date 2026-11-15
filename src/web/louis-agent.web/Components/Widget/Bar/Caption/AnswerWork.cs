using LouisAgent.Web.Chats;
using LouisAgent.Web.Chats.Calls;
using LouisAgent.Web.Usage;

namespace LouisAgent.Web.Components.Widget.Bar.Caption;

/// <summary>
/// What an answer did (F134.5), as the back of its caption shows it: the tools it called in order, how long it thought,
/// and what the framework measured of it, the same usage the ledger holds for its requests.
/// </summary>
/// <remarks>
/// Ask the type: the caption asks this record and only draws it, so reading a turn's work lives in one place with its
/// own tests. It reads only what the turn already holds; nothing here is worked out or guessed.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record AnswerWork(IReadOnlyList<AnswerTool> Tools, double Thought, TurnUsage? Usage)
{
    /// <summary>What this turn did: its tool calls, its thinking time together, and its usage.</summary>
    public static AnswerWork Of(Turn turn)
    {
        var tools = turn.Blocks.Where(block => block.Kind == BlockKind.Tool).Select(ToolOf).ToList();
        double thought = turn.Blocks.Sum(block => block.ThinkingSeconds ?? 0);
        return new AnswerWork(tools, thought, turn.Usage);
    }

    /// <summary>A call by its name, and what it was about: a shown place's address, or an offer's question.</summary>
    private static AnswerTool ToolOf(Block call) =>
        new(call.ToolName ?? "", ShowPageCall.PathOf(call) ?? OfferCall.Of(call)?.Question);
}
