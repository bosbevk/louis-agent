using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Template.Evals.Example.Scoring;

/// <summary>
/// The eval's score function (docs/guides/add-an-eval-set.md, section 4): one trial's transcript as <see cref="ExampleScores"/>. Pure, so it is tested on made-up
/// transcripts. Write it.
/// </summary>
public static class ExampleScorer
{
    public static ExampleScores Score(Trial trial) => throw new NotImplementedException();
}
