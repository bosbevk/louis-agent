using LouisAgent.Composition.Sessions;

namespace LouisAgent.Core.Tests.Composition.Sessions;

/// <summary>
/// F127.2 (Louis's pick "the identity says so"): who a turn runs as, as its tools can read it: the session's identity
/// and whether that identity is a demonstration, so a demonstration's booking is marked a demo by the tool that sends
/// it, with no setting to keep in step.
/// </summary>
public class TurnIdentityTests
{
    /// <summary>An identity is no demonstration unless it says so.</summary>
    [Test]
    public void AnIdentity_IsNoDemonstration_UnlessItSaysSo() =>
        Assert.That(TurnIdentity.Of(new FakeIdentity("guide")), Is.EqualTo(new TurnIdentity("guide", false)));

    /// <summary>One that says it is, is.</summary>
    [Test]
    public void ADemonstrationIdentity_SaysSo() =>
        Assert.That(TurnIdentity.Of(new FakeIdentity("seemeeu-guesthouse", demonstration: true)),
            Is.EqualTo(new TurnIdentity("seemeeu-guesthouse", true)));

    /// <summary>A turn's identity holds for what runs inside it, and the one before comes back after.</summary>
    [Test]
    public void ATurnsIdentity_HoldsInside_AndTheOneBeforeComesBack()
    {
        var outer = new TurnIdentity("guide", false);
        var inner = new TurnIdentity("seemeeu-guesthouse", true);
        TurnIdentity? during;

        using (TurnIdentity.Use(outer))
        {
            using (TurnIdentity.Use(inner))
                during = TurnIdentity.Current;
            Assert.That(TurnIdentity.Current, Is.EqualTo(outer));
        }

        Assert.Multiple(() =>
        {
            Assert.That(during, Is.EqualTo(inner));
            Assert.That(TurnIdentity.Current, Is.Null);
        });
    }
}
