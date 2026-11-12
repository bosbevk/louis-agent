using LouisAgent.Api.Messages;
using LouisAgent.Api.Sessions;
using LouisAgent.SessionStream.Wire.Requests;
using LouisAgent.Testing.Messages;

namespace LouisAgent.Api.Tests.Sessions;

/// <summary>
/// The prompt the API builds from a message (F115): the visitor's words, then their attached files, then the page
/// they are reading as one block, cut at 6,000 characters with a note, so the model sees what the visitor sees.
/// </summary>
public class TurnStreamPromptTests
{
    /// <summary>
    /// A message with a page ends with the page's block: where the visitor is, and what the page says.
    /// </summary>
    [Test]
    public void APage_BecomesABlockAfterTheWords()
    {
        var request = new SendMessageRequest("which page am I on?", null,
            new PageRequest("/build-your-own", "Build your own", "A few lines of your own."));

        string prompt = TurnStream.BuildPrompt(request);

        Assert.That(prompt, Is.EqualTo("which page am I on?\n\n" +
                                       "[The visitor is reading /build-your-own (Build your own). " +
                                       "The page says:\nA few lines of your own.]"));
    }

    /// <summary>A long page is cut at 6,000 characters and the block says so.</summary>
    [Test]
    public void ALongPage_IsCut_AndSaysSo()
    {
        var request = new SendMessageRequest("hi", null, new PageRequest("/packs", "Packs", new string('x', 7_000)));

        string prompt = TurnStream.BuildPrompt(request);

        Assert.Multiple(() =>
        {
            Assert.That(prompt, Does.Contain(new string('x', 6_000) + "]"));
            Assert.That(prompt, Does.Not.Contain(new string('x', 6_001)));
            Assert.That(prompt, Does.Contain("[Page truncated: showing 6,000 of 7,000 chars]"));
        });
    }

    /// <summary>
    /// A page's places end the block (F117): each by its own address and heading, one a line (ADR 0107), so the model
    /// knows where it can send the visitor and names it as it read it.
    /// </summary>
    [Test]
    public void ThePagesAreas_EndTheBlock()
    {
        var request = new SendMessageRequest("where are the levels?", null,
            new PageRequest("/identities", "Identities", "Who an agent is.",
            [
                new PageAreaRequest("/identities/parts", "What an identity holds"),
                new PageAreaRequest("/identities/levels", "The three that exist")
            ]));

        string prompt = TurnStream.BuildPrompt(request);

        Assert.That(prompt, Does.EndWith("The page says:\nWho an agent is.\nIts areas:\n" +
                                         "/identities/parts What an identity holds\n" +
                                         "/identities/levels The three that exist]"));
    }

    /// <summary>
    /// A page's links follow its areas (FX120, Louis: "agent cannot navigate it"): each by the address it opens and its
    /// words, so the guide can take the visitor where a link on their screen leads.
    /// </summary>
    [Test]
    public void ThePagesLinks_FollowItsAreas()
    {
        var request = new SendMessageRequest("show me the triage one", null,
            new PageRequest("/demos", "Demos", "Watch the agents at work.", [new PageAreaRequest("/demos", "Demos")],
                Links:
                [
                    new PageAreaRequest("/triage-demo", "Watch it"),
                    new PageAreaRequest("/contact", "Ask for yours")
                ]));

        string prompt = TurnStream.BuildPrompt(request);

        Assert.That(prompt, Does.EndWith(
            "Its areas:\n/demos Demos\nIts links:\n/triage-demo Watch it\n/contact Ask for yours]"));
    }

    /// <summary>
    /// The page says which section is on the visitor's screen (F118, Louis: "see it did not know where I was"), so
    /// "this" in their message is that section.
    /// </summary>
    [Test]
    public void TheSectionInView_IsNamed()
    {
        var request = new SendMessageRequest("what is this?", null,
            new PageRequest("/", "louis-agent", "An AI agent framework.",
                [new PageAreaRequest("/home/built", "Built on it")],
                new PageAreaRequest("/home/built", "Built on it")));

        string prompt = TurnStream.BuildPrompt(request);

        Assert.That(prompt, Does.EndWith("/home/built Built on it\nOn their screen now: the section /home/built, " +
                                         "\"Built on it\", of the page / they are reading]"));
    }

    /// <summary>
    /// A later message from the same page carries only where they are on it (F118): the page's words went with the
    /// first, and the place in view is one short line that says it is a section of that page, so an address like
    /// /home/contact is never taken for the Contact page (Louis: "that is the contact section, the agent got
    /// confused").
    /// </summary>
    [Test]
    public void OnTheSamePage_OnlyTheSectionInViewTravels()
    {
        var request = new SendMessageRequest("contact page?", null,
            new PageRequest("/", null, null, null,
                new PageAreaRequest("/home/contact", "Building an agent for a .NET team?")));

        string prompt = TurnStream.BuildPrompt(request);

        Assert.That(prompt, Is.EqualTo("contact page?\n\n[On their screen now: the section /home/contact, " +
                                       "\"Building an agent for a .NET team?\", of the page /, " +
                                       "whose words came with an earlier message]"));
    }

    /// <summary>
    /// A card open on the visitor's screen is named with the section (FX106, Louis's screenshot: on the leads pack's
    /// card, "tell me about this pack" got the .NET pack the guide had opened before), so "this" is the card they
    /// opened.
    /// </summary>
    [Test]
    public void AnOpenCard_IsNamed_AsWhatThisMeans()
    {
        var card = new PageAreaRequest("/packs/louis-agent-pack-leads", "louis-agent.pack.leads");
        var first = new SendMessageRequest("tell me about this pack", null,
            new PageRequest("/packs", "Packs", "The packs.", [], new PageAreaRequest("/packs/general", "General tools"),
                card));
        var later = new SendMessageRequest("tell me about this pack", null,
            new PageRequest("/packs", null, null, null, new PageAreaRequest("/packs/general", "General tools"), card));

        string open = "Open on it: the card /packs/louis-agent-pack-leads, \"louis-agent.pack.leads\". When they say " +
                      "\"this\", they mean that card.";
        Assert.Multiple(() =>
        {
            Assert.That(TurnStream.BuildPrompt(first), Does.EndWith($"of the page /packs they are reading\n{open}]"));
            Assert.That(TurnStream.BuildPrompt(later),
                Does.EndWith($"whose words came with an earlier message\n{open}]"));
        });
    }

    /// <summary>
    /// F127.7, Louis: one guide for the whole site, selling, the demos or the framework by where the visitor is. The
    /// page's focus ends its block in one line, the whole page's and a later message's alike, so the guide reads that
    /// part's rules.
    /// </summary>
    [Test]
    public void APagesFocus_IsSaid_WithTheWholePage_AndLater()
    {
        var first = new SendMessageRequest("how does a pack load?", null,
            new PageRequest("/framework/packs", "Packs", "The packs.", Focus: "framework"));
        var later = new SendMessageRequest("and this?", null,
            new PageRequest("/framework/packs", null, null, null, new PageAreaRequest("/framework/packs/web", "Web"),
                Focus: "framework"));

        Assert.Multiple(() =>
        {
            Assert.That(TurnStream.BuildPrompt(first),
                Does.EndWith("The page says:\nThe packs.]\n[The part of the site: framework.]"));
            Assert.That(TurnStream.BuildPrompt(later),
                Does.EndWith("earlier message]\n[The part of the site: framework.]"));
        });
    }

    /// <summary>A page without a focus says none, as before.</summary>
    [Test]
    public void APageWithoutAFocus_SaysNone() =>
        Assert.That(TurnStream.BuildPrompt(new SendMessageRequest("hi", null, new PageRequest("/", "Home", "Hello."))),
            Does.Not.Contain("part of the site"));

    /// <summary>ADR 0119: each of the API's message records matches its template.</summary>
    [Test]
    public void EveryHolesRecord_MatchesItsTemplate() =>
        Assert.That(MessageHoles.Problems([typeof(ApiModelText), typeof(ApiLogMessage)]), Is.Empty);

    /// <summary>Without a page the prompt is the words alone, as before.</summary>
    [Test]
    public void NoPage_LeavesThePromptAlone()
    {
        Assert.That(TurnStream.BuildPrompt(new SendMessageRequest("hi", null)), Is.EqualTo("hi"));
    }
}
