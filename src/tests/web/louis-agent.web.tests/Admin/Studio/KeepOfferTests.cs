using LouisAgent.Web.Admin.Studio;

namespace LouisAgent.Web.Tests.Admin.Studio;

/// <summary>
/// After a person grades a conversation, the panel offers to keep it: a pass as a reference, a fail as a known-bad,
/// an unsure (or no grade yet) as neither.
/// </summary>
public class KeepOfferTests
{
    /// <summary>Each verdict offers its own kind of transcript, or none.</summary>
    [TestCase("pass", KeepOffer.Reference)]
    [TestCase("fail", KeepOffer.KnownBad)]
    [TestCase("unsure", KeepOffer.None)]
    [TestCase(null, KeepOffer.None)]
    public void AVerdict_OffersItsKind(string? verdict, KeepOffer offer) =>
        Assert.That(KeepOffers.For(verdict), Is.EqualTo(offer));
}
