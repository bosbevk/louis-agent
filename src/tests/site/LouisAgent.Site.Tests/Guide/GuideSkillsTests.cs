using LouisAgent.Choices;
using LouisAgent.Composition.Prompts;
using System.Reflection;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Identities.Guide;
using LouisAgent.Packs.Site;
using LouisAgent.Site.Pages;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Site.Tests.Guide;

/// <summary>
/// F129 (ADR 0114): who Louis is and how he answers stay in his voice, in every prompt; the map of the docs and the
/// site's pages are his skills, read on demand, so a message that needs neither doesn't carry them.
/// </summary>
public class GuideSkillsTests
{
    private const string Folder = "agents/louis-agent-guide/";

    /// <summary>Where the guide's skills are, beside its voice.</summary>
    private const string SkillsFolder = "agents/louis-agent-guide/skills";

    /// <summary>
    /// A prompt file of the guide's, split into its header and the body the model reads, its line breaks made one
    /// kind, so a checkout on any machine reads the same.
    /// </summary>
    private static PromptFile Read(string path)
    {
        using var reader = new StreamReader(new GuideIdentity().Prompts.GetFileInfo(Folder + path).CreateReadStream());
        return PromptHeaders.Split(Folder + path, reader.ReadToEnd().ReplaceLineEndings("\n"));
    }

    /// <summary>One of the guide's skills, by its name: the <c>SKILL.md</c> in its folder (CL68.1).</summary>
    private static PromptFile Skill(string name) => Read($"skills/{name}/{PromptFileName.Skill.Wire()}");

    /// <summary>Each map is a skill read on demand, under its own heading.</summary>
    [TestCase("docs-map", "# Where things are")]
    [TestCase("site-map", "# The site you sit on")]
    public void AMap_IsASkillReadOnDemand(string skill, string heading)
    {
        var file = Skill(skill);

        Assert.That((file.Header?.Load, file.Body.StartsWith(heading, StringComparison.Ordinal)),
            Is.EqualTo(((SkillLoad?)SkillLoad.OnDemand, true)));
    }

    /// <summary>
    /// Every page of the site is in the index he always has, so he can open the right one without reading the map
    /// first (F129's live check: with the whole map on demand he stopped opening the framework's pages).
    /// </summary>
    [Test]
    public void EveryPage_IsInTheIndexHeAlwaysHas()
    {
        var index = Skill("site-pages");
        var addresses = typeof(HomePage).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith(typeof(HomePage).Namespace!) == true)
            .SelectMany(type => type.GetCustomAttributes<RouteAttribute>())
            .Select(route => $"`{route.Template}`").ToList();

        Assert.That(index.Header?.Load, Is.EqualTo(SkillLoad.Always));
        Assert.That(addresses, Has.Count.GreaterThanOrEqualTo(9), "the pages were found");
        Assert.That(addresses.Where(address => !index.Body.Contains(address)), Is.Empty);
    }

    /// <summary>
    /// Running a demo live is a skill he always has (F127.4.2): it names every business he may ask with its demo's
    /// page, and how it goes: offered as cards, asked in the visitor's own words, answered in the business's window,
    /// and retold from its work.
    /// </summary>
    [Test]
    public void TheLiveDemo_IsASkillHeAlwaysHas_ForEveryBusinessHeMayAsk()
    {
        var skill = Skill("live-demo");
        string body = string.Join(' ', skill.Body.Split((char[])[' ', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries));

        Assert.Multiple(() =>
        {
            Assert.That(skill.Header?.Load, Is.EqualTo(SkillLoad.Always));
            Assert.That(new GuideIdentity().Calls.Where(address => !body.Contains($"`{address}`")), Is.Empty);
            Assert.That(body, Does.Contain("`Offer`").And.Contain("their own words").And.Contain("`AskAgent`")
                .And.Contain("its own window").And.Contain("`ReadAgentWork`"));
        });
    }

    /// <summary>
    /// FX133, from Louis's look at the live site: walking a demo page never offered the live demo. Every stop on a demo
    /// page now offers it, and after the business answers he coaches: what it just did and why (Louis's pick, "run it
    /// live and coach").
    /// </summary>
    [Test]
    public void AtWhatToWatch_TheWalkthroughOffersTheDemoLive_AndHeCoachesAfterTheAnswer()
    {
        string walk = Flat(Skill("walk-a-page").Body);
        string skill = Flat(Skill("live-demo").Body);

        Assert.Multiple(() =>
        {
            Assert.That(walk, Does.Contain(
                    "On a demo page, every stop's choices include a fourth, Ask it live, until they have asked live"),
                "Louis: drop the card drawn in code, rework the prompt");
            Assert.That(skill, Does.Contain("what it just did and why"));
            Assert.That(skill, Does.Contain("end with `Offer`: two or three questions they could ask it next"),
                "Louis: \"when he is the teacher he must give me offers of what I could ask\"");
        });
    }

    /// <summary>
    /// FX139, Louis: "the guide should always display options, even to go to another page after it was done". The
    /// guide ends each answer with Offer, the tool the web draws as cards, so the engine asks for it when it's missing.
    /// </summary>
    [Test]
    public void TheGuide_EndsEachAnswerWithChoices() =>
        Assert.That(new GuideIdentity().EndsEachAnswerWith, Is.EqualTo(LouisAgent.Web.Chats.Calls.OfferCall.Tool));

    /// <summary>
    /// Louis, on "Stop here" answered with choices again: "stop should end"; his pick, the engine knows a stop. The
    /// guide stops on the card's words, the walkthrough's choice says exactly those, and after it the guide says
    /// goodbye and offers nothing.
    /// </summary>
    [Test]
    public void StopHere_Ends()
    {
        using var reader = new StreamReader(new SitePack().Prompts.GetFileInfo("toolsets/site.md").CreateReadStream());
        string siteGuide = reader.ReadToEnd();
        string walk = Skill("walk-a-page").Body;

        Assert.Multiple(() =>
        {
            Assert.That(new GuideIdentity().StopsOn, Is.EqualTo(new[] { "Stop here" }));
            Assert.That(siteGuide, Does.Contain("or Stop here, in those words"));
            Assert.That(walk, Does.Contain("When they say Stop here, say goodbye in a line and offer nothing"));
        });
    }

    /// <summary>
    /// FX140, Louis on Ask it live: "it should have given me a prompt for seemeeu". The guide answered with cards that
    /// only said "type my question"; it now offers questions to ask the business, each sent as it is when clicked.
    /// </summary>
    [Test]
    public void OnAskItLive_TheGuideOffersQuestionsForTheBusiness()
    {
        string skill = Flat(Skill("live-demo").Body);

        Assert.Multiple(() =>
        {
            Assert.That(skill, Does.Contain("offer two or three questions to ask the business"));
            Assert.That(skill, Does.Contain("never a card that only says to type"));
            Assert.That(skill, Does.Contain("answer it in its window, and offer nothing"),
                "FX141, Louis: \"it is waiting for my name, so now no card should display\"");
        });
    }

    /// <summary>
    /// FX144, Louis, of the guide telling him to carry on and book after the booking card in the window said Sent: "it
    /// should have known"; his pick, a rule to read the work. Whenever the visitor writes to him after a live ask, he
    /// reads the business's work first, so he never answers from what he last saw.
    /// </summary>
    [Test]
    public void AfterALiveAsk_HeReadsTheWorkBeforeAnswering() =>
        Assert.That(Flat(Skill("live-demo").Body), Does.Contain(
            "Whenever they write to you again after you asked a business live, call `ReadAgentWork` with its address " +
            "first"));

    /// <summary>
    /// F134: on the showroom's start each demo is a window that is a picture of it, and no business answers there.
    /// Offered Ask it live there, the guide asked a business that had no window on the page, and told the visitor to
    /// answer in a window they could not see. So a walk of the start offers to open a demo in its place, and the
    /// guide takes the visitor to the demo's own page before it asks a business anything.
    /// </summary>
    [Test]
    public void OnTheStart_TheGuideOpensADemo_AndAsksABusinessOnlyOnItsOwnPage()
    {
        string walk = Flat(Skill("walk-a-page").Body);
        string live = Flat(Skill("live-demo").Body);
        string showroom = Flat(Skill("focus-demos").Body);

        Assert.Multiple(() =>
        {
            Assert.That(walk, Does.Contain(
                "On the showroom's start, a window's stop offers Open this demo in its place"));
            Assert.That(live, Does.Contain("Never call `AskAgent` on any page but that business's own demo page"));
            Assert.That(live, Does.Contain("take them to its demo page with `ShowPage` first"));
            Assert.That(showroom, Does.Contain("a picture of its demo").And.Contain("Open this demo"));
        });
    }

    /// <summary>
    /// Louis, of arriving in a demo with only "Walk me through this demo" and a bare "Ask it live": the guide "should
    /// guide me in the door with options to what to ask". So on arrival the guide's choices are questions to put to
    /// the business, ready to press, with the walk and the stop after them.
    /// </summary>
    [Test]
    public void ArrivingInADemo_TheGuideOffersQuestionsToAskTheBusiness()
    {
        string showroom = Flat(Skill("focus-demos").Body);

        Assert.Multiple(() =>
        {
            Assert.That(showroom, Does.Contain("two questions to ask the business"));
            Assert.That(showroom, Does.Contain("then Walk me through this demo, and Stop here"));
            Assert.That(showroom, Does.Contain("call `AskAgent` as your live-demo skill says"));
        });
    }

    /// <summary>
    /// A live ask is watched in the business's window. The guide once lit another part of the page in the same
    /// answer, which scrolled that window off the screen as the business answered. So with a live ask it shows the
    /// demo itself, where the window is, and nothing else.
    /// </summary>
    [Test]
    public void AskingLive_TheGuideKeepsTheBusinesssWindowOnScreen() =>
        Assert.That(Flat(Skill("live-demo").Body), Does.Contain(
            "In an answer where you call `AskAgent`, the only place you may show is the demo itself"));

    /// <summary>
    /// F160, Louis: "do not make it autoplay, give the ability to our agent to guide him through the experience", and
    /// "when it is done it can ask if you want to try and then it guides the user into the chat with the other
    /// agent". The guide walks a demo's conversation a turn at a time, says what to watch before each turn plays, and
    /// after the last asks whether they want to try it, then hands them to the business live.
    /// </summary>
    [Test]
    public void OnADemoPage_TheGuidePlaysTheConversationATurnAtATime_ThenOffersToTryIt()
    {
        string walk = Flat(Skill("walk-a-page").Body);
        string skill = Flat(Skill("live-demo").Body);
        string showroom = Flat(Skill("focus-demos").Body);

        Assert.Multiple(() =>
        {
            Assert.That(skill, Does.Contain("showing a turn plays it, and it stops after it"));
            Assert.That(skill, Does.Contain("say first, in two short sentences, what to watch for in it"));
            Assert.That(skill, Does.Contain("After the last turn, ask whether they want to try it themselves"));
            Assert.That(walk, Does.Contain("a demo page's conversation is walked a turn at a time"));
            Assert.That(showroom, Does.Contain("the conversation a turn at a time"));
        });
    }

    /// <summary>
    /// Every skill of Orbi's shows one worked example (Louis, 2026-11-18: "a skill should always include references
    /// and an example"): an example shows the model the style and the level of detail better than a rule alone.
    /// </summary>
    [Test]
    public void EverySkill_ShowsAnExample()
    {
        var skills = new GuideIdentity().Prompts.GetDirectoryContents(SkillsFolder).Select(file => file.Name);

        Assert.That(skills.Where(name => !Skill(name).Body.Contains("\n## An example\n")), Is.Empty);
    }

    /// <summary>
    /// Every skill of Orbi's has the skill shape (CL64, Louis: "we have to rework orbi's skills also"): the rules it
    /// never breaks come first, seven at most, so they are read before anything else, and it ends with its example
    /// and the docs it rests on, which Orbi can open to check.
    /// </summary>
    [Test]
    public void EverySkill_HasTheSkillShape()
    {
        const int MostNevers = 7;
        var skills = new GuideIdentity().Prompts.GetDirectoryContents(SkillsFolder).Select(file => file.Name);

        var wrong = skills.Where(name =>
        {
            string body = Skill(name).Body;
            int never = body.IndexOf("\n## Never\n", StringComparison.Ordinal);
            int example = body.IndexOf("\n## An example\n", StringComparison.Ordinal);
            int references = body.IndexOf("\n## References\n", StringComparison.Ordinal);
            int nextAfterNever = never < 0 ? -1 : body.IndexOf("\n## ", never + 1, StringComparison.Ordinal);
            string nevers = never < 0 || nextAfterNever < 0 ? "" : body[never..nextAfterNever];
            int count = nevers.Split('\n').Count(line => line.Length > 2 && char.IsDigit(line[0]) && line[1] == '.');

            return never < 0 || never > example || example > references || count is 0 or > MostNevers;
        });

        Assert.That(wrong, Is.Empty, "Never first (1 to 7 rules), then the example, then the references");
    }

    /// <summary>A prompt's text on one line, so a rule can be found whatever its line breaks.</summary>
    private static string Flat(string text) =>
        string.Join(' ', text.Split((char[])[' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// Orbi writes like a writer (Louis, 2026-11-18: "give orbi a writer skill in order to type better", his pick of a
    /// skill of its own, always on): short sentences, no colon, semicolon or bracket, one idea a paragraph, the point
    /// first, active voice and plain words. The voice keeps who Orbi is, and no longer holds the writing rules.
    /// </summary>
    [Test]
    public void Writing_IsASkillHeAlwaysHas_ApartFromTheVoice()
    {
        var skill = Skill("writing");
        string body = string.Join(' ', skill.Body.Split((char[])[' ', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries));

        Assert.Multiple(() =>
        {
            Assert.That(skill.Header?.Load, Is.EqualTo(SkillLoad.Always));
            Assert.That(body, Does.StartWith("# How you write"));
            Assert.That(body, Does.Contain("about 14 words").And.Contain("never more than 25")
                .And.Contain("No colon").And.Contain("no semicolon").And.Contain("no brackets")
                .And.Contain("One idea to a paragraph").And.Contain("Lead with the answer")
                .And.Contain("Active voice").And.Contain("Read it aloud"));
            Assert.That(Read("voice.md").Body, Does.Not.Contain("**Plain words, short.**"));
        });
    }

    /// <summary>The voice keeps who he is and how he answers, and no longer carries either map.</summary>
    [Test]
    public void TheVoice_KeepsWhoHeIs_WithoutTheMaps() =>
        Assert.That(Read("voice.md").Body, Does.Contain("## How you answer").And.Contain("## What stays Louis's")
            .And.Not.Contain("## Where things are").And.Not.Contain("## The site you sit on"));
}
