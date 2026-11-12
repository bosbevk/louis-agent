using LouisAgent.Web.Components.Site.Pieces;

namespace LouisAgent.Web.Tests.Components.Site.Pieces;

/// <summary>
/// F127.7: a piece of a page a visitor can select has an id from its title, lower case, its words joined by dashes,
/// marked as a piece, so it never clashes with a section's id.
/// </summary>
public class PieceIdTests
{
    /// <summary>The title's words, lower case, joined by dashes, after "piece-".</summary>
    [TestCase("It answers while you sleep", "piece-it-answers-while-you-sleep")]
    [TestCase("  It knows your business! ", "piece-it-knows-your-business")]
    [TestCase("R500 a month, hosted", "piece-r500-a-month-hosted")]
    [TestCase("Ask it live: the guide's way in", "piece-ask-it-live-the-guide-s-way-in")]
    public void APiecesId_IsItsTitle(string title, string id) => Assert.That(PieceId.Of(title), Is.EqualTo(id));
}
