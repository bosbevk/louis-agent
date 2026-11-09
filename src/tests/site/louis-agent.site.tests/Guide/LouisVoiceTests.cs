using LouisAgent.Identities.Guide;

namespace LouisAgent.Site.Tests.Guide;

/// <summary>
/// The guide's voice is Louis (F128.1, Louis: "we must give louis-agent a name, Louis"): the AI version of the man who
/// made louis-agent, with his own story and his way of talking, and it says it is the AI whenever that matters, since
/// the principles every agent reads say it may never lie.
/// </summary>
public class LouisVoiceTests
{
    /// <summary>
    /// The voice as the engine reads it, from the identity's own prompts, its lines joined: a wrapped line in Markdown
    /// is still one sentence.
    /// </summary>
    private static string Voice()
    {
        using var reader = new StreamReader(new GuideIdentity().Prompts
            .GetFileInfo("agent/louis-agent-guide/voice.md").CreateReadStream());
        return string.Join(' ', reader.ReadToEnd().Split((char[])[' ', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>It is Louis, and the AI version of him.</summary>
    [Test]
    public void TheVoice_IsLouis_TheAIVersion() =>
        Assert.That(Voice(), Does.Contain("You are Louis").And.Contain("the AI version of Louis"));

    /// <summary>His story is the one he told, and nothing is added to it.</summary>
    [TestCase("Malmesbury")]
    [TestCase("Yzerfontein")]
    [TestCase("Swartland")]
    [TestCase("four kids")]
    [TestCase("more than twenty years")]
    [TestCase("works remotely")]
    [TestCase("Stellenbosch")]
    [TestCase("Never add to his story")]
    public void TheVoice_TellsHisStory(string words) =>
        Assert.That(Voice(), Does.Contain(words));

    /// <summary>Asked whether it is a person, it says what it is and where the real one is.</summary>
    [Test]
    public void AskedIfItIsAPerson_ItSaysItIsTheAI() =>
        Assert.That(Voice(), Does.Contain("Never say or suggest you are a person").And.Contain("WhatsApp"));

    /// <summary>
    /// It helps first and doesn't push (Louis: "a bit too eager to pass on details and get the sale, we should tone it
    /// down a bit"): no pitch in every answer, and no asking for details before the visitor wants to go ahead.
    /// </summary>
    [Test]
    public void TheVoice_DoesNotPush()
    {
        string voice = Voice();

        Assert.Multiple(() =>
        {
            Assert.That(voice, Does.Contain("you don't push").And.Contain("Most answers need no pitch at all"));
            Assert.That(voice,
                Does.Contain("Never ask for a name, a number or an address until they've said they want"));
            Assert.That(voice, Does.Not.Contain("take the lead").And.Not.Contain("one small nudge"));
        });
    }

    /// <summary>
    /// Three short sentences at most (Louis: "keep the response to max 3 short sentences, only when the visitor asks to
    /// explain or give more detail then explain long").
    /// </summary>
    [Test]
    public void AnAnswer_IsThreeShortSentences_UnlessTheyAskForMore() =>
        Assert.That(Voice(), Does.Contain("Every answer is three short sentences or fewer")
            .And.Contain("Only when the visitor asks you to explain, or for more detail, do you go long"));

    /// <summary>
    /// The first answer of a chat greets, a different greeting each time (Louis: "I like it to be a random greeting but
    /// it should greet"), since a tour starts the chat before the widget's own greeting shows.
    /// </summary>
    [Test]
    public void TheFirstAnswer_Greets() =>
        Assert.That(Voice(), Does.Contain("Your first answer of a chat opens with a greeting")
            .And.Contain("Pick a different one each time"));

    /// <summary>
    /// It talks like him: plain, at home, dry, with an idiom where one helps, and no more bubbly guide.
    /// </summary>
    [Test]
    public void TheVoice_TalksLikeHim()
    {
        string voice = Voice();

        Assert.Multiple(() =>
        {
            Assert.That(voice, Does.Contain("feel at home").And.Contain("dry").And.Contain("idiom"));
            Assert.That(voice, Does.Contain("be Louis when you talk")
                .And.Contain("you speak proper English and proper Afrikaans, not formal but proper, never a mix"));
            Assert.That(voice, Does.Not.Contain("lekker").And.Not.Contain("sommer"));
            Assert.That(voice, Does.Contain("you're fun to talk to").And.Contain("Never flat"));
            Assert.That(voice, Does.Contain("You're excited about what you sell")
                .And.Contain("Excited about the product, never pushy about the sale"));
            Assert.That(voice, Does.Not.Contain("bubbly"));
        });
    }
}
