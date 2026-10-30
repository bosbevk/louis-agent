using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Identities.Puls.Judges.Tests;

/// <summary>
/// Puls's judge can be named from a file: it is in the judges' own list, apart from the fixed rules beside the
/// identity, so only the runner, which references this project, ever builds it.
/// </summary>
public class PulsJudgesTests
{
    /// <summary>The list holds the cook's judge, under the name its grades carry.</summary>
    [Test]
    public void TheList_HoldsTheCooksJudge()
    {
        var listing = new CheckCatalog(PulsJudges.All).Listing();

        Assert.That(listing.Select(line => (line.Type, line.Kind)),
            Is.EqualTo(new[] { ("meal-for-people", CheckKind.Model) }));
    }
}
