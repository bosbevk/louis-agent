using LouisAgent.Choices;
using LouisAgent.Identities.Puls;
using LouisAgent.Puls.Text;

namespace LouisAgent.Puls.Tests.Text;

// F18 step 7.5.4c (ADR 0043: a copied name is checked across the boundary). The web app keeps its own enum of the roles it has words for, because it cannot
// reference the identity; a role it names that the identity does not offer would quietly stop showing its phase labels. This test sees both and holds the copy to the original.
public class PulsWebRoleTests
{
    [Test]
    public void EveryRoleTheWebAppHasWordsFor_IsARoleTheIdentityOffers() =>
        Assert.That(
            Enum.GetValues<PulsWebRole>().Select(r => r.Wire())
                .Except(Enum.GetValues<PulsRole>().Select(r => r.Wire())), Is.Empty);
}
