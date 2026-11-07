using LouisAgent.Api.Evals.Drafts.Transcripts;
using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Evals.Protocol.Parts.Conversations;

namespace LouisAgent.Api.Evals.Tests.Evals.Drafts.Transcripts;

/// <summary>
/// A reviewed conversation is kept as a transcript only when people's verdicts say what it is: a reference needs
/// every decided verdict to be a pass, a known-bad at least one fail, and a conversation nobody passed or failed
/// (only unsure, or no review) offers neither. The lines are kept whole, under the name the eval folders use.
/// </summary>
public class TranscriptDraftsTests
{
    /// <summary>When the draft was made.</summary>
    private static readonly DateTimeOffset At = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);

    /// <summary>The conversation the transcript comes from.</summary>
    private static readonly ConversationKey CookAf = new(SampleMode.Beta, 1, "cook-af");

    /// <summary>Two protocol lines, one with Afrikaans and a quote in it.</summary>
    private static readonly string[] Lines =
    [
        """{"event":"mode","mode":"beta"}""",
        """{"event":"turn","answer":"Hier is \"bobotie\" vir vier — ê"}"""
    ];

    /// <summary>Keeps the conversation as the given kind, over these decided verdicts.</summary>
    private static TranscriptDraft Keep(TranscriptKind kind, string? reason, params HumanVerdict[] verdicts) =>
        TranscriptDrafts.Of("d1", "cook", SampleIdentity.Alpha, "run-1", CookAf, verdicts, kind, reason, Lines, "Louis", At);

    /// <summary>A passed conversation is kept as a reference, named for its scenario, its lines whole.</summary>
    [Test]
    public void APassedConversation_IsKeptAsAReference_WithItsLinesWhole()
    {
        var draft = Keep(TranscriptKind.Reference, null, HumanVerdict.Pass);

        Assert.That((draft.FileName, draft.Kind, draft.Reason),
            Is.EqualTo(("cook-af.reference.txt", TranscriptKind.Reference, (string?)null)));
        Assert.That(draft.Lines, Is.EqualTo(Lines));
        Assert.That((draft.Id, draft.Eval, draft.Identity, draft.Run, draft.Conversation, draft.Author, draft.At),
            Is.EqualTo(("d1", "cook", SampleIdentity.Alpha, "run-1", CookAf, "Louis", At)));
    }

    /// <summary>A failed conversation is kept as a known-bad, its reason in its name.</summary>
    [Test]
    public void AFailedConversation_IsKeptAsAKnownBad_ItsReasonInItsName() =>
        Assert.That(Keep(TranscriptKind.Bad, "side-dish", HumanVerdict.Pass, HumanVerdict.Fail).FileName,
            Is.EqualTo("cook-af.bad.side-dish.txt"));

    /// <summary>A reference with a fail among its verdicts, or a known-bad with none, is refused.</summary>
    [Test]
    public void TheKindMustMatchTheVerdicts()
    {
        var reference = Assert.Throws<ArgumentException>(() =>
            Keep(TranscriptKind.Reference, null, HumanVerdict.Pass, HumanVerdict.Fail));
        var bad = Assert.Throws<ArgumentException>(() => Keep(TranscriptKind.Bad, "side-dish", HumanVerdict.Pass));

        Assert.That(reference!.Message, Does.Contain("pass"));
        Assert.That(bad!.Message, Does.Contain("fail"));
    }

    /// <summary>A conversation with no decided verdict (only unsure, or none) offers neither.</summary>
    [TestCase(TranscriptKind.Reference)]
    [TestCase(TranscriptKind.Bad)]
    public void AConversationNobodyDecided_OffersNeither(TranscriptKind kind)
    {
        var refused = Assert.Throws<ArgumentException>(() => Keep(kind, "side-dish"));

        Assert.That(refused!.Message, Does.Contain("cook-af"));
    }

    /// <summary>A reason must be kebab-case, since it becomes part of a file's name.</summary>
    [TestCase("Side dish")]
    [TestCase("side_dish")]
    [TestCase("-side")]
    [TestCase("side--dish")]
    [TestCase("../side")]
    public void AReasonThatIsNotKebabCase_IsRefused(string reason) =>
        Assert.Throws<ArgumentException>(() => Keep(TranscriptKind.Bad, reason, HumanVerdict.Fail));
}
