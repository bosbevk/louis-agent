using LouisAgent.Choices;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Conformance;

// F18 step 7.5.4 (ADR 0043): the stub eval's modes are an enum of its own; the runner's protocol and the stored runs carry their text.
public class StubModeTests
{
    [Test]
    public void TheModesTheStubRunsIn_TravelUnderTheirRecordedNames() =>
        Assert.That(Enum.GetValues<StubMode>().Select(m => m.Wire()), Is.EqualTo(new[] { "puls-only", "puls-cook" }));
}
