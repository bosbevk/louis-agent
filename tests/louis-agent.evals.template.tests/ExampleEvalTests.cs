using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Template.Evals.Example;
using LouisAgent.Evals.Template.Evals.Example.Scoring;
using LouisAgent.Evals.Template.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Template.Tests;

// F18 step 5.4c: the template is a starting point, not a broken project. What works (the dataset, the names, the grader's shape) is proven working; what is
// left to write throws NotImplementedException, and a test says so, so the next person reads what is left from the tests they will turn green.
public class ExampleEvalTests
{
    private static readonly ExampleEval Eval = new();

    private static readonly EvalMode AnyMode = new("example", "example/voice", []);

    private static readonly Trial AnyTrial = new("run_1", AnyMode, 1,
        new EvalScenario("example", new EvalInput(null, "Etc/UTC", ["x"]), default, [], DatasetSplit.Dev), new Transcript([]));

    [Test]
    public void TheSetListsItsOneEval()
    {
        var set = new TemplateEvalSet();

        Assert.That(set.Identity, Is.EqualTo(TemplateIdentity.Template.Wire()));
        Assert.That(set.Evals.Select(e => e.Name), Is.EqualTo(new[] { TemplateEvalName.Example.Wire() }));
    }

    [Test]
    public void ItsNameIsAMemberOfTheSetsOwnEnum_UnderTheNameTheGuideShows() =>
        Assert.That(Enum.GetValues<TemplateEvalName>().Select(n => n.Wire()), Is.EqualTo(new[] { "template-example" }));

    [Test]
    public void ItsDatasetLoads_WithItsGrader_SoTheLoadersRulesAreShownWorking()
    {
        var scenario = Eval.Scenarios.Single();

        Assert.That((scenario.Id, scenario.Split), Is.EqualTo(("example", DatasetSplit.Dev)));
        Assert.That(scenario.Expected.GetProperty("example").GetBoolean(), Is.True);
    }

    [Test]
    public void ItHasANameAVersionAndAPortalTitleFromAMessageEnum()
    {
        Assert.That((Eval.Name, Eval.Version), Is.EqualTo((TemplateEvalName.Example.Wire(), 1)));
        Assert.That(Eval.Proves, Is.EqualTo(MessageText.Format(TemplateMessage.ExampleProves)).And.Not.Empty);
    }

    [Test]
    public void ItsGraderIsARequiredFixedRule_ThatReadsTheKeyTheScenarioSets()
    {
        var grader = Eval.Graders.Single();

        Assert.That((grader.Name, grader.Kind, grader.Required), Is.EqualTo(("example", GraderKind.FixedRule, true)));
        Assert.That(grader.ExpectedKeys, Is.EqualTo(new[] { "example" }));
    }

    [Test]
    public void WhatIsLeftToWrite_ThrowsNotImplemented()
    {
        // Each is a member the next person writes: the modes, the facts of a turn, the grader's rule, the scorer.
        Assert.Throws<NotImplementedException>(() => Eval.ModesAsync(null!, CancellationToken.None));
        Assert.Throws<NotImplementedException>(() => Eval.TurnFactsOf(new TurnTranscript(1, "s", "q", [], TimeSpan.Zero, null), AnyMode));
        Assert.Throws<NotImplementedException>(() => Eval.Graders.Single().GradeAsync(AnyTrial, CancellationToken.None));
        Assert.Throws<NotImplementedException>(() => ExampleScorer.Score(AnyTrial));
        Assert.Throws<NotImplementedException>(() => Eval.ScoresOf(AnyTrial, []));
    }
}
