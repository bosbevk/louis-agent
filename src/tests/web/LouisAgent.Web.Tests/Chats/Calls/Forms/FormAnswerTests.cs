using LouisAgent.Web.Chats.Calls.Forms;

namespace LouisAgent.Web.Tests.Chats.Calls.Forms;

/// <summary>
/// What a filled form sends (F127.4.3): each field by its label, in the form's order, as one message the agent reads
/// and the visitor sees as their own; and the form read back from that message, so the card can fold to what was sent.
/// </summary>
public class FormAnswerTests
{
    /// <summary>The guesthouse's form: a name and a way to reach the guest.</summary>
    private static readonly Asked Contact = new("Who shall I put the request under?",
        [new AskedField("Name", AskedKind.Name), new AskedField("Phone or email", AskedKind.Text)]);

    /// <summary>A custom-agent job told in the visitor's words, which may hold anything.</summary>
    private static readonly Asked Job = new("What is the job?",
        [new AskedField("The job", AskedKind.LongText), new AskedField("Email", AskedKind.Email)]);

    /// <summary>Each field by its label, trimmed, in order, joined with a dot.</summary>
    [Test]
    public void AFilledForm_IsOneMessage()
    {
        string[] typed = [" John ", "066 391 2470"];

        Assert.That(FormAnswer.Of(Contact, typed), Is.EqualTo("Name: John · Phone or email: 066 391 2470"));
    }

    /// <summary>The message reads back into the form's values, even a long answer that holds a dot and lines.</summary>
    [Test]
    public void TheMessage_ReadsBack()
    {
        string[] typed = ["Read our invoices · file them\nand tell finance", "ann@example.co.za"];
        string sent = FormAnswer.Of(Job, typed);

        Assert.That(FormAnswer.Read(Job, sent), Is.EqualTo(typed));
    }

    /// <summary>A visitor who wrote their own words didn't send the form: nothing reads back.</summary>
    [Test]
    public void TheirOwnWords_ReadBackAsNothing() =>
        Assert.That(FormAnswer.Read(Contact, "Can I bring two dogs?"), Is.Null);

    /// <summary>
    /// The web's kinds are the site pack's, by name: the pack's tests hold the same list, since neither may reference
    /// the other (ADR 0027).
    /// </summary>
    [Test]
    public void TheKinds_AreThePacks() =>
        Assert.That(Enum.GetNames<AskedKind>(), Is.EqualTo(new[] { "Name", "Phone", "Email", "Text", "LongText" }));
}
