using LouisAgent.Web.Evals.Words;
using LouisAgent.Web.Evals.Words.Parts;

namespace LouisAgent.Web.Evals.Tests.Words;

/// <summary>Every part of the eval pages has its words, and a count says one or many as it should (ADR 0132).</summary>
public class EvalsWordsTests
{
    /// <summary>No part of the pages is left without its words.</summary>
    [Test]
    public void EveryPart_HasItsWords() =>
        Assert.That(Enum.GetValues<EvalsPart>().Where(part => string.IsNullOrWhiteSpace(EvalsWords.Words[part])),
            Is.Empty);

    /// <summary>One is said in the one's words, any other count in the many's.</summary>
    [Test]
    public void ACount_SaysOneOrMany()
    {
        Assert.That(EvalsWords.Count(1, EvalsPart.Repeat, EvalsPart.Repeats), Is.EqualTo("1 repeat"));
        Assert.That(EvalsWords.Count(3, EvalsPart.Repeat, EvalsPart.Repeats), Is.EqualTo("3 repeats"));
        Assert.That(EvalsWords.Count(0, EvalsPart.Conversation, EvalsPart.Conversations),
            Is.EqualTo("0 conversations"));
    }

    /// <summary>A time is said in its two largest parts, from seconds alone up to hours and minutes.</summary>
    [Test]
    public void ATime_IsSaidInItsTwoLargestParts()
    {
        Assert.That(EvalsWords.FormatTime(TimeSpan.FromSeconds(33)), Is.EqualTo("33 s"));
        Assert.That(EvalsWords.FormatTime(new TimeSpan(0, 6, 12)), Is.EqualTo("6 min 12 s"));
        Assert.That(EvalsWords.FormatTime(new TimeSpan(1, 2, 5)), Is.EqualTo("1 h 2 min"));
    }
}
