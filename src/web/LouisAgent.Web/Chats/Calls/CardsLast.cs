using LouisAgent.Web.Chats.Calls.Forms;

namespace LouisAgent.Web.Chats.Calls;

/// <summary>
/// An answer's blocks in the order the chat draws them (F127.4.1): its words and work as they came, then the cards it
/// drew and the choices it offered, in their own order, so the cards always sit under the words that explain them.
/// </summary>
/// <remarks>
/// Louis's pick on the phone check, of three (keep the model's order and trust the prompt; put the words in the Offer
/// call): a small model sometimes draws the card before it writes, and the chat should read right whatever it does.
/// Only the drawing moves: the turn's blocks, and the history the model reads, keep the order they were made in.
/// </remarks>
public static class CardsLast
{
    /// <summary>The blocks with the cards and the choices moved under everything else.</summary>
    public static IEnumerable<Block> Of(IEnumerable<Block> blocks)
    {
        var all = blocks.ToList();
        return all.Where(block => !IsCard(block)).Concat(all.Where(IsCard));
    }

    /// <summary>A call the chat draws as a card: a ShowCard, an Offer or (F127.4.3) an AskFor.</summary>
    private static bool IsCard(Block block) =>
        block.Kind == BlockKind.Tool && block.ToolName is CardCall.Tool or OfferCall.Tool or AskForCall.Tool;
}
