using LouisAgent.Web.Chats.Calls.Forms;

namespace LouisAgent.Web.Chats.Calls.Answers;

/// <summary>
/// A choice that holds a box, read as a form of one field (FX147): its words go as a form's answer does,
/// <c>Tell me about your business: A guesthouse in Paternoster</c>, and an answer is known as written in it the
/// same way. Its label is the agent's ask, as a form's is (FX152).
/// </summary>
public static class OfferWriting
{
    /// <summary>The choice as a form of its one field, under the offer's question.</summary>
    public static Asked FormOf(string question, OfferChoice choice)
    {
        var field = new AskedField(choice.Label, choice.Writes ?? AskedKind.Text);
        return new Asked(question, [field]);
    }

    /// <summary>Whether the visitor's message was written in this choice's box.</summary>
    public static bool Answers(string question, OfferChoice choice, string answer) =>
        choice.Writes is not null && FormAnswer.Read(FormOf(question, choice), answer) is not null;
}
