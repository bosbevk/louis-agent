using LouisAgent.Choices;
using LouisAgent.Evals.Runner;
using LouisAgent.Evals.Runner.Arguments;
using LouisAgent.Evals.Runner.Catalog;

namespace LouisAgent.Evals.Runner.Tests.Arguments;

// EVALS= picks the sets as IDENTITIES picks identities (F18 step 4.1): comma separated, case ignored, each name once; unset means
// every set. Written first: the parser threw NotImplementedException on the red run.
public class EvalSetSelectionTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    public void UnsetMeansEverySet(string? value) =>
        Assert.That(EvalSetSelection.Parse(value), Is.EqualTo(Enum.GetValues<EvalSetKind>()));

    [Test]
    public void NamesAreCaseIgnoredAndEachOnce() =>
        Assert.That(EvalSetSelection.Parse(" PULS, puls "), Is.EqualTo(new[] { EvalSetKind.Puls }));

    [Test]
    public void RefusesAnUnknownSetNamingTheSupportedOnes() =>
        Assert.That(() => EvalSetSelection.Parse("pulss"),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("'pulss'").And.Message
                .Contains(EvalSetKind.Puls.Wire()));
}
