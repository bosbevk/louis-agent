using LouisAgent.Choices;
using LouisAgent.Composition.Prompts;
using LouisAgent.Identities.Guide;

namespace LouisAgent.Site.Tests.Guide;

/// <summary>
/// The guide's voice is Orbi, Louis's AI agent (Louis, 2026-11-18: "Orbi it is"): it tells the story of the man who
/// made louis-agent as his, talks the way he does, and says it is an AI whenever that matters, since the principles
/// every agent reads say it may never lie.
/// </summary>
public class OrbiVoiceTests
{
    /// <summary>
    /// The voice as the engine reads it, from the identity's own prompts, its lines joined: a wrapped line in Markdown
    /// is still one sentence.
    /// </summary>
    private static string Voice() => Flat("voice.md");

    /// <summary>One of Orbi's skills, read the same way as the voice.</summary>
    private static string Skill(string name) => Flat($"skills/{name}/{PromptFileName.Skill.Wire()}");

    /// <summary>A prompt file of Orbi's, its lines joined, so a rule is found whatever its line breaks.</summary>
    private static string Flat(string path)
    {
        using var reader = new StreamReader(new GuideIdentity().Prompts
            .GetFileInfo($"agents/louis-agent-guide/{path}").CreateReadStream());
        return string.Join(' ', reader.ReadToEnd().Split((char[])[' ', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>It is Orbi, Louis's AI agent, and his story is told as his, never as its own.</summary>
    [Test]
    public void TheVoice_IsOrbi_LouisAIAgent() =>
        Assert.That(Voice(), Does.Contain("You are Orbi, Louis's AI agent")
            .And.Contain("tell it as his, never as yours")
            .And.Not.Contain("the AI version of Louis"));

    /// <summary>
    /// A decision is cards, never a question in words (F127.4.1, Louis: "would have given me three options"): every
    /// walkthrough stop ends with three choices, other choices go as cards too, and a lead is shown as a card and sent
    /// only on its click. A stop shows its area on its own and waits, so the card never goes out before the words
    /// (FX253).
    /// </summary>
    [TestCase("walk-a-page", "end with `Offer` and three choices: on to the next stop, by its name (suggested)")]
    [TestCase("walk-a-page", "Show that area with `ShowPage`, on its own, and wait for it to answer.")]
    [TestCase("writing", "so it goes last, after your words, in the same reply.")]
    [TestCase("walk-a-page", "Never ask it in words.")]
    [TestCase("take-a-lead", "Show the three back with `ShowCard`")]
    [TestCase("take-a-lead", "When Send it comes back, call the tool once")]
    public void TheSkills_OfferChoicesAsCards(string skill, string words) =>
        Assert.That(Skill(skill), Does.Contain(words));

    /// <summary>Any other choice in the voice goes as cards too.</summary>
    [Test]
    public void TheVoice_OffersChoicesAsCards() =>
        Assert.That(Voice(), Does.Contain("**Choices as cards.**"));

    /// <summary>His story is the one he told, and nothing is added to it.</summary>
    [TestCase("Malmesbury")]
    [TestCase("Yzerfontein")]
    [TestCase("Swartland")]
    [TestCase("four kids")]
    [TestCase("more than twenty years")]
    [TestCase("works remotely")]
    [TestCase("Stellenbosch")]
    [TestCase("Paarl and the Winelands")]
    [TestCase("Never add to his story")]
    public void TheVoice_TellsHisStory(string words) =>
        Assert.That(Voice(), Does.Contain(words));

    /// <summary>Asked whether it is a person, it says it is an AI, and where Louis is.</summary>
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
            .And.Contain("Greet by the time of day: call `GetToday` before your first answer")
            .And.Contain("then say who you are")
            .And.Contain("A proper greeting, never slang")
            .And.Not.Contain("Howzit"));

    /// <summary>
    /// The greeting follows the part of the day the clock names (FX127): at 01:40 it said "good morning" in one chat
    /// and "good evening" in the next, and at night it is "good evening".
    /// </summary>
    [Test]
    public void TheGreeting_FollowsThePartOfTheDay_TheClockNames() =>
        Assert.That(Voice(), Does.Contain("by the part of the day it names").And.Contain("at night, good evening"));

    /// <summary>
    /// A visitor never gets a decision record's number (FX127): the guide once told one about "ADR 0027".
    /// </summary>
    [Test]
    public void AVisitor_GetsNoDecisionNumbers() =>
        Assert.That(Voice(), Does.Contain("no ADR or its number"));

    /// <summary>
    /// It sounds like Louis: plain, at home, dry, with an idiom where one helps, and no more bubbly guide.
    /// </summary>
    [Test]
    public void TheVoice_TalksLikeHim()
    {
        string voice = Voice();

        Assert.Multiple(() =>
        {
            Assert.That(voice, Does.Contain("feel at home").And.Contain("dry").And.Contain("idiom"));
            Assert.That(voice, Does.Contain("sound like Louis when you talk")
                .And.Contain("you speak proper English and proper Afrikaans, not formal but proper, never a mix"));
            Assert.That(voice, Does.Not.Contain("lekker").And.Not.Contain("sommer"));
            Assert.That(voice, Does.Contain("you're fun to talk to").And.Contain("Never flat"));
            Assert.That(voice, Does.Contain("You're excited about what you sell")
                .And.Contain("Excited about the product, never pushy about the sale"));
            Assert.That(voice, Does.Not.Contain("bubbly"));
        });
    }

    /// <summary>
    /// F127.4.3, Louis on a "Describe the job" card that only said to type: "see this should also be an input card".
    /// Whatever he needs them to write, a lead's details or a job, he asks with a form, and never offers a choice that
    /// only says to type.
    /// </summary>
    [Test]
    public void WhatTheyMustWrite_HeAsksWithAForm()
    {
        string voice = Voice();

        Assert.Multiple(() =>
        {
            Assert.That(voice, Does.Contain("never a choice that only says to type"));
            Assert.That(Skill("take-a-lead"), Does.Contain("one `AskFor` form, only what they haven't told you"));
            Assert.That(voice, Does.Contain("ask for it with `AskFor`, one field The job (LongText)"));
            Assert.That(voice, Does.Not.Contain("one thing at a time, as a person would"));
        });
    }

    /// <summary>
    /// Louis, on "Talk to the real Louis" answered with WhatsApp and the contact page: "it should have offered to take
    /// my lead". Wanting to talk to him is a lead too: he offers to pass their details on with the form, and names
    /// WhatsApp and the contact page as the other way.
    /// </summary>
    [Test]
    public void WantingToTalkToLouis_IsALead() =>
        Assert.That(Skill("take-a-lead"), Does.Contain("or wants to talk to Louis")
            .And.Contain("Offer to pass it on yourself"));

    /// <summary>
    /// Before a visitor sends their details, he says what Send it does and where the privacy page is (F145.1), so
    /// they agree knowing what happens to it, as POPIA asks.
    /// </summary>
    [Test]
    public void BeforeALeadIsSent_HeSaysWhatSendingDoes() =>
        Assert.That(Skill("take-a-lead"), Does.Contain("say in one plain sentence what Send it does")
            .And.Contain("the privacy page says what happens to it"));

    /// <summary>
    /// FX228: the offer of two free websites is closed (FX227, Louis, 2026-11-23: "remove this then asap"), so nothing
    /// Orbi reads names it: not its voice, not a skill, and the skill that took an entry is gone.
    /// </summary>
    [Test]
    public void TheFreeWebsites_AreClosed_OrbiNoLongerKnowsThem()
    {
        string agent = Path.Combine(TestPaths.RepoRoot, "src", "site", "LouisAgent.Identities.Guide",
            PromptFolder.Agents.Wire(), "louis-agent-guide");
        var naming = Directory.EnumerateFiles(agent, "*.md", SearchOption.AllDirectories)
            .Where(file => File.ReadAllText(file).Contains("free website", StringComparison.OrdinalIgnoreCase) ||
                           File.ReadAllText(file).Contains("free-website", StringComparison.OrdinalIgnoreCase))
            .Select(file => Path.GetRelativePath(agent, file));

        Assert.That(naming, Is.Empty);
        Assert.That(Directory.Exists(Path.Combine(agent, PromptFolder.Skills.Wire(), "free-website-entry")), Is.False);
    }

    /// <summary>
    /// The voice works like CLAUDE.md (Louis, 2026-11-18: "why is our voice file not working like claude.md, why is
    /// it not referencing skills"): a table names each on-demand skill by the job it is for, so Orbi reads it then.
    /// </summary>
    [Test]
    public void TheVoice_NamesEverySkillItReadsOnDemand()
    {
        var identity = new GuideIdentity();
        var onDemand = identity.Prompts.GetDirectoryContents("agents/louis-agent-guide/skills")
            .Select(folder => folder.Name)
            .Where(name => Skill(name).Contains("load: on-demand"));

        Assert.That(onDemand.Where(name => !Voice().Contains($"`louis-agent-guide/{name}`")), Is.Empty);
    }

    /// <summary>
    /// FX231: a question that asks several things at once gets the first in three short sentences and the rest as
    /// choices, one thing at a time; going long is for explaining one thing in depth (Louis's pick, "One part at a
    /// time").
    /// </summary>
    [Test]
    public void AskedSeveralThingsAtOnce_ItAnswersTheFirst_AndOffersTheRest() =>
        Assert.That(Voice(), Does.Contain("asks several things at once"));

    /// <summary>
    /// FX231: the first answer's greeting doesn't count toward the three short sentences; the three come after it
    /// (Louis, of a judge that counted it: "greeting does not count as a short sentence, so that is correct judge is
    /// not").
    /// </summary>
    [Test]
    public void TheFirstAnswersGreeting_DoesNotCount_TheThreeComeAfterIt() =>
        Assert.That(Voice(),
            Does.Contain("The greeting doesn't count").And.Not.Contain("the greeting is one of the three"));

    /// <summary>
    /// FX232: no doc is named anywhere in an answer, by its title or its path, and asked where to read, the guide says
    /// what the docs teach and offers to take them through it; it had said "the guide called Build a pack".
    /// </summary>
    [Test]
    public void ADoc_IsNamedNowhere_ByItsTitleOrItsPath() =>
        Assert.That(Voice(), Does.Contain("by its title or its path, anywhere in an answer"));

    /// <summary>
    /// FX233: words come before a choice card, never a card alone. Offer ends the answer, so a guide that called it
    /// first sent no words at all, once in three in its baseline.
    /// </summary>
    [Test]
    public void ACard_ComesAfterTheWords_NeverAlone() =>
        Assert.That(Voice() + Skill("writing"),
            Does.Contain("Write your words first, then call `Offer`").And.Contain("Never an answer without words"));
}
