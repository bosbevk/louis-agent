using LouisAgent.Choices;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Shared;

// F18 step 7.5.4c (ADR 0043: a copied name is checked across the boundary). The eval set keeps its own enum of the roles its evals name, because a set never references
// an identity; a role it expects that the identity does not offer would make a run grade against nothing. This test sees both and holds the copy to the original.
public class PulsEvalRoleTests
{
    [Test]
    public void EveryRoleThePulsEvalsName_IsARoleTheIdentityOffers() =>
        Assert.That(
            Enum.GetValues<PulsEvalRole>().Select(r => r.Wire())
                .Except(Enum.GetValues<PulsRole>().Select(r => r.Wire())), Is.Empty);
}
