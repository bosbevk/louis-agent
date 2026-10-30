using LouisAgent.Choices;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Template.Evals.Example;
using LouisAgent.Evals.Template.Messages;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Template.Tests;

/// <summary>
/// The template is a starting point, not a broken project (F18 step 5.4c). What works (the dataset, the names, the
/// grader's shape) is shown working; what's left to write throws, and a test says so, so the next person reads what's
/// left from the tests they'll turn green.
/// </summary>
public class ExampleEvalTests
{
    /// <summary>The template's one eval.</summary>
    private static readonly ExampleEval Eval = new();

    /// <summary>Any mode will do: the members that take one throw before they read it.</summary>
    private static readonly EvalMode AnyMode = new("example", "example/voice", []);

    /// <summary>Any trial will do, for the same reason.</summary>
    private static readonly Trial AnyTrial = new("run_1", AnyMode, 1,
        new EvalScenario("example", new EvalInput(null, "Etc/UTC", ["x"]), default, [], DatasetSplit.Dev),
        new Transcript([]));

    /// <summary>The set lists its one eval, under the template's identity.</summary>
    [Test]
    public void TheSetListsItsOneEval()
    {
        var set = new TemplateEvalSet();

        Assert.That(set.Identity, Is.EqualTo(TemplateIdentity.Template.Wire()));
        Assert.That(set.Evals.Select(e => e.Name), Is.EqualTo(new[] { TemplateEvalName.Example.Wire() }));
    }

    /// <summary>The eval's name comes from the set's own enum, as the guide shows it.</summary>
    [Test]
    public void ItsNameIsAMemberOfTheSetsOwnEnum_UnderTheNameTheGuideShows() =>
        Assert.That(Enum.GetValues<TemplateEvalName>().Select(n => n.Wire()), Is.EqualTo(new[] { "template-example" }));

    /// <summary>Its dataset loads, so the loader's rules are shown working.</summary>
    [Test]
    public void ItsDatasetLoads_WithItsGrader_SoTheLoadersRulesAreShownWorking()
    {
        var scenario = Eval.Scenarios.Single();

        Assert.That((scenario.Id, scenario.Split), Is.EqualTo(("example", DatasetSplit.Dev)));
        Assert.That(scenario.Expected.GetProperty("example").GetBoolean(), Is.True);
    }

    /// <summary>It has a name, a version and a portal title that's a message.</summary>
    [Test]
    public void ItHasANameAVersionAndAPortalTitleFromAMessageEnum()
    {
        Assert.That((Eval.Name, Eval.Version), Is.EqualTo((TemplateEvalName.Example.Wire(), 1)));
        Assert.That(Eval.Proves, Is.EqualTo(MessageText.Format(TemplateMessage.ExampleProves)).And.Not.Empty);
    }

    /// <summary>Its grader is a required fixed rule that reads the key the scenario sets.</summary>
    [Test]
    public void ItsGraderIsARequiredFixedRule_ThatReadsTheKeyTheScenarioSets()
    {
        var grader = Eval.Checks.Single();

        Assert.That((grader.Name, grader.Kind, grader.Required), Is.EqualTo(("example", CheckKind.FixedRule, true)));
        Assert.That(grader.ExpectedKeys, Is.EqualTo(new[] { "example" }));
    }

    /// <summary>The modes, a turn's facts and the grader's rule are left to write, and say so.</summary>
    [Test]
    public void WhatIsLeftToWrite_ThrowsNotImplemented()
    {
        Assert.Throws<NotImplementedException>(() => Eval.ModesAsync(null!, CancellationToken.None));
        Assert.Throws<NotImplementedException>(() =>
            Eval.TurnFactsOf(new TurnTranscript(1, "s", "q", [], TimeSpan.Zero, null), AnyMode));
        Assert.Throws<NotImplementedException>(() =>
            Eval.Checks.Single().GradeAsync(AnyTrial, CancellationToken.None));
    }
}
