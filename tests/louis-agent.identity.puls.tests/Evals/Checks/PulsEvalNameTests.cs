using LouisAgent.Choices;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks;

/// <summary>
/// The names of Puls's evals are an enum of the checks' own; the portal, the store and a run's protocol carry their
/// text, and the eval's file says it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public class PulsEvalNameTests
{
    /// <summary>The cook's file names the eval as the enum does, under the name its stored runs carry.</summary>
    [Test]
    public void TheCooksFile_NamesItAsTheEnumDoes_UnderItsRecordedName()
    {
        Assert.That(CookFile.Eval.Name, Is.EqualTo(PulsEvalName.Cook.Wire()));
        Assert.That(Enum.GetValues<PulsEvalName>().Select(n => n.Wire()), Is.EqualTo(new[] { "puls-cook" }));
    }
}
