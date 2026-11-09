using LouisAgent.Choices;
using LouisAgent.Packs.Triage.Runs.Steps;
using LouisAgent.Packs.Triage.Web.Runs;

namespace LouisAgent.Packs.Triage.Web.Tests.Runs;

/// <summary>
/// Each kind of step has its plain words for a visitor (F106.3.4): what that kind of step is, beside what this one
/// did. Every kind the pack can write has its own, so a new kind can't reach the page unexplained.
/// </summary>
public class StepWordsTests
{
    /// <summary>Every kind of step the pack writes has words of its own, no two the same, and no semicolon.</summary>
    [Test]
    public void EveryKind_HasItsOwnWords()
    {
        var words = Enum.GetValues<TriageStepKind>().Select(kind => StepWords.Of(kind.Wire())).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(words, Has.All.Not.Empty);
            Assert.That(words, Is.Unique);
            Assert.That(words, Has.None.Contain(";"));
            Assert.That(words, Has.None.EqualTo(StepWords.Of("not-a-kind")));
        });
    }
}
