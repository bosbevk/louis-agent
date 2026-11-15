using LouisAgent.Web.Chats;
using LouisAgent.Web.Chats.Calls;
using LouisAgent.Web.Chats.Calls.Forms;

namespace LouisAgent.Web.Components.Widget.Bar.Caption;

/// <summary>
/// What the agent's caption shows of an answer (F134.4): its words, the last choices it offered, and on its back how it
/// was made (F134.5). Every answer with something to say stands as a caption, as the design has it, whether or not it
/// showed a place. One that did stands beside the place, and the rest over the bar. The one kind that can't is an
/// answer holding what only the whole talk draws.
/// </summary>
/// <remarks>
/// Ask the type: the bar asks this record whether an answer is a caption and what it holds, so the rule lives in one
/// place beside the caption that draws it, and the turn itself stays a plain record of what was streamed.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
/// <param name="Words">What it said. An answer with no words of its own says what it asked over its choices.</param>
/// <param name="Question">What it asked over its choices, when that is not already its words.</param>
/// <param name="Offer">The last choices it offered; none when it offered none.</param>
/// <param name="Work">How the answer was made, for the caption's back.</param>
/// <param name="Beside">Whether it showed a place on the page, which the caption then stands beside.</param>
public sealed record Captioned(string Words, string? Question, Offered? Offer, AnswerWork Work, bool Beside)
{
    /// <summary>The caption of this answer, or none when it needs the whole talk or has nothing to say yet.</summary>
    public static Captioned? Of(Turn? turn)
    {
        if (turn is null || NeedsTheTalk(turn)) return null;
        var offer = turn.Blocks.Select(OfferCall.Of).LastOrDefault(offered => offered is not null);
        var work = AnswerWork.Of(turn);

        // With words, the question stands quietly over the choices. With none, the question is all it said.
        if (turn.Words.Length > 0) return new Captioned(turn.Words, offer?.Question, offer, work, turn.ShowedAPlace);
        return offer is null ? null : new Captioned(offer.Question, null, offer, work, turn.ShowedAPlace);
    }

    /// <summary>
    /// A caption holds words and choices. An answer that went wrong, asks with a form or drew a card needs the whole
    /// talk, where those are drawn.
    /// </summary>
    public static bool NeedsTheTalk(Turn? turn) =>
        turn is not null && (turn.Error is not null || turn.Blocks.Any(OnlyTheTalkDraws));

    /// <summary>A form to fill in and a drawn card are the talk's to draw.</summary>
    private static bool OnlyTheTalkDraws(Block block) =>
        AskForCall.Of(block) is not null || CardCall.SourceOf(block) is not null;
}
