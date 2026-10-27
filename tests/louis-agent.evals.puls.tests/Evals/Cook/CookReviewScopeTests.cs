using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Shared;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Trials;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook;

// F18 step 6a.4 (Louis): the cook's human review asks "is this a meal for the people asked for?", so it applies only where the meal judge has a meal to judge (the scenario has `serves`).
// The control (control-web) and the ask-first scenario ask for no number of people: no pending review, no panel. Written first: the review applied to every scenario.
public class CookReviewScopeTests
{
    private static readonly CookEval Eval = new();

    private static bool Applies(string scenario) =>
        Eval.Graders.OfType<HumanReviewGrader>().Single().AppliesTo(
            new Trial("run_1", new EvalMode(CookMode.Cook.Wire(), "puls/voice-af", [PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire()]), 1, Eval.Scenarios.Single(s => s.Id == scenario), new Transcript([])));

    [TestCase("control-web", false)]
    [TestCase("cook-asks-first", false)]
    [TestCase(PulsCookScenarioIds.CookEn, true)]
    [TestCase(PulsCookScenarioIds.CookAf, true)]
    public void TheReviewAppliesWhereTheMealJudgeHasAMealToJudge(string scenario, bool applies) =>
        Assert.That(Applies(scenario), Is.EqualTo(applies));
}
