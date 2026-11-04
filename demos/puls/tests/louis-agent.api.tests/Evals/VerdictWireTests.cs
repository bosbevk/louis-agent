using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Choices;

namespace LouisAgent.Api.Tests.Evals;

/// <summary>
/// A person's verdict has one word everywhere: the store keeps it and the portal's API sends it as pass, fail or
/// unsure, lower case like every wire value (7c's pick C). The protocol's kinds are the contract's and travel in its
/// kebab-case words (F18 step 7c.3).
/// </summary>
public class VerdictWireTests
{
    /// <summary>A verdict is written in lower case, the one word the store and the API share.</summary>
    [Test]
    public void AVerdict_IsWrittenInLowerCase() =>
        Assert.That(Enum.GetValues<HumanVerdict>().Select(v => v.Wire()),
            Is.EqualTo(new[] { "pass", "fail", "unsure" }));
}
