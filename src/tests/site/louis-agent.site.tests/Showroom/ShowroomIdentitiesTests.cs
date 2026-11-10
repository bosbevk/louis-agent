using LouisAgent.Composition.Packs;
using LouisAgent.Identities.Showroom;
using LouisAgent.SiteApi;

namespace LouisAgent.Site.Tests.Showroom;

/// <summary>
/// Three made-up businesses' agents for the site's demos (F128.2): a restaurant, a law practice and, since F127.1, a
/// guesthouse in place of the doctor's practice (Louis: "we should just change doctor to something else"), each a
/// voice with its business's facts and the leads pack to pass a booking on. The practice's agent never advises, the
/// guesthouse's never promises a room, every agent says it is an AI, and the site's host loads them after Louis.
/// </summary>
public class ShowroomIdentitiesTests
{
    /// <summary>The three, by their own names.</summary>
    private static readonly IAgentIdentity[] Businesses =
        [new RestaurantIdentity(), new LawPracticeIdentity(), new GuesthouseIdentity()];

    /// <summary>A voice, its lines joined, as the engine reads it.</summary>
    private static string Voice(IAgentIdentity identity)
    {
        using var reader = new StreamReader(identity.Prompts.GetFileInfo($"agent/{identity.DefaultVoice}.md")
            .CreateReadStream());
        return string.Join(' ', reader.ReadToEnd().Split((char[])[' ', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Each is named for its business, finds its voice, and uses the leads pack to pass a booking on and the site pack
    /// to show its website (F127.1, Louis: "the agent can navigate the website basically").
    /// </summary>
    [TestCase(0, "wheatfield-kitchen")]
    [TestCase(1, "koringland-attorneys")]
    [TestCase(2, "seemeeu-guesthouse")]
    public void ABusiness_HasItsName_ItsVoice_AndTheLeadsAndSitePacks(int at, string name)
    {
        var business = Businesses[at];

        Assert.Multiple(() =>
        {
            Assert.That(business.Name, Is.EqualTo(name));
            Assert.That(business.DefaultVoice, Is.EqualTo($"{name}/voice"));
            Assert.That(business.Prompts.GetFileInfo($"agent/{name}/voice.md").Exists, Is.True);
            Assert.That(business.Packs, Is.EqualTo(new[] { "leads", "site" }));
        });
    }

    /// <summary>
    /// Every voice shows the visitor its website (Louis: "first show them the room and then they book it"): a place
    /// that answers what they ask is opened with ShowPage before the answer.
    /// </summary>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void EveryVoice_ShowsItsWebsite_BeforeItAnswers(int at) =>
        Assert.That(Voice(Businesses[at]), Does.Contain("open it with `ShowPage` first"));

    /// <summary>
    /// Every voice says it is an AI, is made up, and passes a booking on only after the customer's yes.
    /// </summary>
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void EveryVoice_IsHonest_AndBooksOnlyAfterAYes(int at)
    {
        string voice = Voice(Businesses[at]);

        Assert.Multiple(() =>
        {
            Assert.That(voice, Does.Contain("You are an AI"));
            Assert.That(voice, Does.Contain("made-up business"));
            Assert.That(voice, Does.Contain("only after they say yes"));
            Assert.That(voice, Does.Contain("Three short sentences at most"));
        });
    }

    /// <summary>
    /// What each business's agent must not do: the practice's never advises, the guesthouse's never promises a room
    /// it can't see; each books and passes on.
    /// </summary>
    [TestCase(1, "never give legal advice")]
    [TestCase(2, "Never promise a room")]
    [TestCase(2, "Dogs")]
    public void EachVoice_KnowsWhatItMustNotDo(int at, string words) =>
        Assert.That(Voice(Businesses[at]), Does.Contain(words));

    /// <summary>The site's host loads them after Louis, so he stays the default and the one open to visitors.</summary>
    [Test]
    public void TheSitesCatalogue_HasLouisFirst_ThenTheThree()
    {
        Assert.That(SiteCatalog.Identities.Select(identity => identity.Name),
            Is.EqualTo(new[]
            {
                "louis-agent-guide", "wheatfield-kitchen", "koringland-attorneys", "seemeeu-guesthouse"
            }));
    }
}
