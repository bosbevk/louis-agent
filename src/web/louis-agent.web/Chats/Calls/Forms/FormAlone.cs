namespace LouisAgent.Web.Chats.Calls.Forms;

/// <summary>
/// An answer's blocks with no choices beside a form (F127.4.3, Louis: "and then only that card"): when the answer
/// asks with a form, the visitor fills that in, so any choices it also offered aren't drawn. A refused form asks
/// nothing, and then the choices stay.
/// </summary>
/// <remarks>
/// Only the drawing changes, as in <see cref="CardsLast"/>: the turn's blocks and the history the model reads keep
/// everything. The site pack's guide already tells the model a form is the only card in its answer; this holds it on
/// the page when a model offers anyway.
/// </remarks>
public static class FormAlone
{
    /// <summary>The blocks, without the choices when a form was asked.</summary>
    public static IEnumerable<Block> Of(IEnumerable<Block> blocks)
    {
        var all = blocks.ToList();
        if (!all.Any(block => AskForCall.Of(block) is not null)) return all;
        return all.Where(block => OfferCall.Of(block) is null);
    }
}
