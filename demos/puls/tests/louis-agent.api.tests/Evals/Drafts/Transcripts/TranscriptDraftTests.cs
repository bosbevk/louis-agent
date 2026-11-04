using LouisAgent.Api.Evals.Drafts.Transcripts;

namespace LouisAgent.Api.Tests.Evals.Drafts.Transcripts;

/// <summary>
/// A kept transcript is named the way the eval folders name theirs, so F49.5 can write it as it is: a reference is
/// <c>{scenario}.reference.txt</c>, a known-bad <c>{scenario}.bad.{reason}.txt</c>, and a known-bad without a reason
/// is refused, since the reason is what the file says went wrong.
/// </summary>
public class TranscriptDraftTests
{
    /// <summary>A reference is named for its scenario, and needs no reason.</summary>
    [Test]
    public void AReference_IsNamedForItsScenario() =>
        Assert.That(TranscriptDraft.FileNameOf("cook-af", TranscriptKind.Reference, null),
            Is.EqualTo("cook-af.reference.txt"));

    /// <summary>A known-bad carries its reason in its name.</summary>
    [Test]
    public void AKnownBad_CarriesItsReasonInItsName() =>
        Assert.That(TranscriptDraft.FileNameOf("cook-af", TranscriptKind.Bad, "side-dish"),
            Is.EqualTo("cook-af.bad.side-dish.txt"));

    /// <summary>A known-bad with no reason, or only spaces, is refused, saying it needs one.</summary>
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void AKnownBadWithoutAReason_IsRefused(string? reason)
    {
        var refused = Assert.Throws<ArgumentException>(() =>
            TranscriptDraft.FileNameOf("cook-af", TranscriptKind.Bad, reason));

        Assert.That(refused!.Message, Does.Contain("reason"));
    }
}
