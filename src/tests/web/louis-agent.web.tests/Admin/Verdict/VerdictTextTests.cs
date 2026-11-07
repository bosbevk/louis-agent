using LouisAgent.Web.Admin.Verdict;

namespace LouisAgent.Web.Tests.Admin.Verdict;

/// <summary>The verdict's words: passed of total, the change against the run before, the answers to grade.</summary>
public class VerdictTextTests
{
    /// <summary>A mode's verdict with this change; first when there is no run before.</summary>
    private static ModeVerdict Mode(int? change, bool first = false) => new(SampleMode.Alpha, 3, 4, change, first);

    /// <summary>Passed of total reads as a short sentence.</summary>
    [Test]
    public void PassedOfTotal_ReadsAsASentence() =>
        Assert.That(VerdictText.Passed(Mode(0)), Is.EqualTo("3 of 4 passed"));

    /// <summary>The change says up, down or the same against the run before, first on a first run.</summary>
    [TestCase(2, false, "2 more than the run before", "is-up")]
    [TestCase(-1, false, "1 fewer than the run before", "is-down")]
    [TestCase(0, false, "the same as the run before", "is-same")]
    [TestCase(null, true, "first run", "is-first")]
    [TestCase(null, false, "the run before has no score", "is-first")]
    public void TheChange_SaysUpDownOrTheSame(int? change, bool first, string text, string tone)
    {
        Assert.That(VerdictText.Change(Mode(change, first)), Is.EqualTo(text));
        Assert.That(VerdictText.Tone(Mode(change, first)), Is.EqualTo(tone));
    }

    /// <summary>The answers to grade count in plain words.</summary>
    [TestCase(1, "1 answer to grade")]
    [TestCase(5, "5 answers to grade")]
    public void TheAnswersToGrade_CountInPlainWords(int count, string text) =>
        Assert.That(VerdictText.ToGrade(count), Is.EqualTo(text));
}
