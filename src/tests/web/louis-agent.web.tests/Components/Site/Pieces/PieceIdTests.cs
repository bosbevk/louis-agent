using LouisAgent.Web.Components.Site.Pieces;

namespace LouisAgent.Web.Tests.Components.Site.Pieces;

/// <summary>
/// F127.7: a piece of a page a visitor or the guide can select has an id from its title, lower case, its words joined
/// by dashes, so its address reads like a card's (ADR 0107): <c>/home/it-answers-while-you-sleep</c>.
/// </summary>
public class PieceIdTests
{
    /// <summary>The title's letters and digits, lower case, every other run one dash.</summary>
    [TestCase("It answers while you sleep", "it-answers-while-you-sleep")]
    [TestCase("  It knows your business! ", "it-knows-your-business")]
    [TestCase("$0.66 → $0.13", "0-66-0-13")]
    [TestCase("100+ ADRs", "100-adrs")]
    [TestCase("Ask it live: the guide's way in", "ask-it-live-the-guide-s-way-in")]
    public void APiecesId_IsItsTitle(string title, string id) => Assert.That(PieceId.Of(title), Is.EqualTo(id));
}
