using LouisAgent.Evals.Puls.Shared;

namespace LouisAgent.Evals.Puls.Tests;

// F18 step 7.5.4f (ADR 0043): an id is data, so the tests hold it as one constant where it repeats, and the constant is held to the dataset it names: a misspelt id fails here, once.
public class PulsIdsTests
{
    private static IEnumerable<string> ConstantsOf(Type ids) =>
        ids.GetFields().Where(f => f.IsLiteral && f.FieldType == typeof(string)).Select(f => (string)f.GetRawConstantValue()!);

    [Test]
    public void EveryRoutingTaskId_IsATaskOfTheRoutingDataset()
    {
        var ids = PulsDatasetsTests.Load(PulsEvalName.Routing).Scenarios.Select(s => s.Id).ToHashSet();

        Assert.That(ConstantsOf(typeof(PulsTaskIds)).Where(id => !ids.Contains(id)), Is.Empty);
    }

    [Test]
    public void EveryCookScenarioId_IsAScenarioOfTheCookDataset()
    {
        var ids = PulsDatasetsTests.Load(PulsEvalName.Cook).Scenarios.Select(s => s.Id).ToHashSet();

        Assert.That(ConstantsOf(typeof(PulsCookScenarioIds)).Where(id => !ids.Contains(id)), Is.Empty);
    }
}
