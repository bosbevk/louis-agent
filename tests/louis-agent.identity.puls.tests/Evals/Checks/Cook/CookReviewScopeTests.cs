using LouisAgent.Choices;
using LouisAgent.Evals;
using LouisAgent.Evals.Grading.Human;
using LouisAgent.Evals.Trials;
using LouisAgent.Identities.Puls.Evals.Checks.Cook;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook;

/// <summary>
/// The cook's human review asks "is this a meal for the people asked for?", so it applies only where the meal judge
/// has a meal to judge (the scenario has <c>serves</c>). The control (<c>control-web</c>) and the ask-first scenario
/// ask for no number of people: no pending review, no panel.
/// </summary>
public class CookReviewScopeTests
{
    /// <summary>Whether the cook's review has a question to ask about a trial of this scenario.</summary>
    private static bool Applies(string scenario) =>
        CookFile.Eval.Checks.OfType<HumanReviewCheck>().Single().AppliesTo(
            new Trial("run_1",
                new EvalMode(CookMode.Cook.Wire(), "puls/voice-af",
                    [PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire()]), 1,
                CookFile.Eval.Scenarios.Single(s => s.Id == scenario), new Transcript([])));

    /// <summary>The review applies where the judge has a meal to judge, and nowhere else.</summary>
    [TestCase("control-web", false)]
    [TestCase("cook-asks-first", false)]
    [TestCase(PulsCookScenarioIds.CookEn, true)]
    [TestCase(PulsCookScenarioIds.CookAf, true)]
    public void TheReviewAppliesWhereTheMealJudgeHasAMealToJudge(string scenario, bool applies) =>
        Assert.That(Applies(scenario), Is.EqualTo(applies));
}
