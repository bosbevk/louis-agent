using LouisAgent.Choices;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Shared;

/// <summary>
/// The checks keep their own enum of the roles they name; a role they expect that the identity doesn't offer would
/// make a run grade against nothing, so this test holds the copy to the original.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public class PulsEvalRoleTests
{
    /// <summary>Every role the checks name is one the Puls identity offers.</summary>
    [Test]
    public void EveryRoleThePulsEvalsName_IsARoleTheIdentityOffers() =>
        Assert.That(
            Enum.GetValues<PulsEvalRole>().Select(r => r.Wire())
                .Except(Enum.GetValues<PulsRole>().Select(r => r.Wire())), Is.Empty);
}
