using LouisAgent.Evals.Puls.Shared;

namespace LouisAgent.Evals.Puls.Tests;

/// <summary>
/// An id is data (ADR 0043), so the tests hold it as one constant where it repeats, and the constant is held to the
/// dataset it names: a misspelt id fails here, once.
/// </summary>
public class PulsIdsTests
{
    /// <summary>The values of a class's string constants.</summary>
    private static IEnumerable<string> ConstantsOf(Type ids) =>
        ids.GetFields().Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!);

    /// <summary>Every cook scenario id the tests name is a scenario of the cook's dataset.</summary>
    [Test]
    public void EveryCookScenarioId_IsAScenarioOfTheCookDataset()
    {
        var ids = PulsDatasetsTests.Load(PulsEvalName.Cook).Scenarios.Select(s => s.Id).ToHashSet();

        Assert.That(ConstantsOf(typeof(PulsCookScenarioIds)).Where(id => !ids.Contains(id)), Is.Empty);
    }
}
