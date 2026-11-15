using LouisAgent.Web.Chats;
using LouisAgent.Web.Chats.Calls;

namespace LouisAgent.Web.Components.Widget.Bar.Caption;

/// <summary>
/// What the agent's caption shows of an answer (F134.4): its words, the last choices it offered, and on its back how it
/// was made (F134.5). An answer stands as a caption when it showed a place and said or offered something; any other
/// answer stays in the talk.
/// </summary>
/// <remarks>
/// Ask the type: the bar asks this record whether an answer is a caption and what it holds, so the rule lives in one
/// place beside the caption that draws it, and the turn itself stays a plain record of what was streamed.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public sealed record Captioned(string Words, Offered? Offer, AnswerWork Work)
{
    /// <summary>The caption of this answer, or none when it showed no place or had nothing to say.</summary>
    public static Captioned? Of(Turn? turn)
    {
        if (turn is not { ShowedAPlace: true }) return null;
        var offer = turn.Blocks.Select(OfferCall.Of).LastOrDefault(offered => offered is not null);
        if (turn.Words.Length == 0 && offer is null) return null;
        return new Captioned(turn.Words, offer, AnswerWork.Of(turn));
    }
}
