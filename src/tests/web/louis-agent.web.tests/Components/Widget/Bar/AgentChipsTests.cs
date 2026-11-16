using LouisAgent.Web.Chats.Calls;
using LouisAgent.Web.Chats.Calls.Forms;
using LouisAgent.Web.Components.Widget.Bar;

namespace LouisAgent.Web.Tests.Components.Widget.Bar;

/// <summary>
/// F134.3: the chips over the bar. Before anything is asked they are the site's own questions. Once there is a talk,
/// they follow it: the site's first question stays, and the rest are what the guide last offered, so a chip is
/// always a next step from where the talk was (Louis: "these 2 button should be generated based on where the last
/// convo was").
/// </summary>
public class AgentChipsTests
{
    private static readonly AgentSuggestions Site =
        new(["Walk me through this page", "Which demo is like my business?", "What does it cost?"]);

    /// <summary>What the guide last offered: three questions, the second suggested, and a box to write in.</summary>
    private static readonly Offered Last = new("What next?",
    [
        new OfferChoice("Is the beach far?"),
        new OfferChoice("Book the garden room", Suggested: true),
        new OfferChoice("Something else", Writes: AskedKind.Text),
        new OfferChoice("Stop here"),
    ]);

    /// <summary>Before any answer the chips are the site's questions, its first the bright one.</summary>
    [Test]
    public void BeforeAnyAnswer_TheChipsAreTheSites()
    {
        var chips = AgentChips.Of(Site, null);

        Assert.That((chips.Questions, chips.Suggested), Is.EqualTo((Site.Questions, "Walk me through this page")));
    }

    /// <summary>
    /// After an answer that offered choices, the site's first question stays and the next two chips are the guide's
    /// own choices, its suggested one first and bright. A choice that is a box to write in is no chip.
    /// </summary>
    [Test]
    public void AfterAnAnswer_TheChipsFollowTheTalk()
    {
        var chips = AgentChips.Of(Site, Last);

        Assert.Multiple(() =>
        {
            Assert.That(chips.Questions.Take(3),
                Is.EqualTo(new[] { "Book the garden room", "Is the beach far?", "Stop here" }));
            Assert.That(chips.Suggested, Is.EqualTo("Book the garden room"));
        });
    }

    /// <summary>
    /// FX173: the guide's own choices take the first places, its suggested one over the orb, and the site's questions
    /// fill the places left, up to the five round the closed orb, none twice (Louis: "the agents own idea must take
    /// priority").
    /// </summary>
    [Test]
    public void TheGuidesOwnIdeas_ComeFirst_TheSitesFillTheRest()
    {
        var chips = AgentChips.Of(Site, Last);

        Assert.That(chips.Questions, Is.EqualTo(new[]
        {
            "Book the garden room", "Is the beach far?", "Stop here", "Walk me through this page",
            "Which demo is like my business?"
        }));
    }

    /// <summary>A site that names no questions has no chips, whatever the guide offered.</summary>
    [Test]
    public void WithNoQuestionsOfTheSites_ThereAreNoChips()
    {
        var none = (AgentChips.Of(null, Last).Questions.Count, AgentChips.Of(new AgentSuggestions([]), null)
            .Questions.Count);

        Assert.That(none, Is.EqualTo((0, 0)));
    }
}
