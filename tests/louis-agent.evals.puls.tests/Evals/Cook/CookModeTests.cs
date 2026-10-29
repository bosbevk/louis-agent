using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Puls.Shared;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook;

/// <summary>
/// The modes the cook eval runs in are an enum of the eval set's own (F18 step 7.5.4); a run's protocol, the store and
/// the portal carry their names.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public class CookModeTests
{
    /// <summary>The modes travel under the names earlier runs recorded.</summary>
    [Test]
    public void TheModesTheCookEvalRunsIn_TravelUnderTheirRecordedNames() =>
        Assert.That(Enum.GetValues<CookMode>().Select(m => m.Wire()), Is.EqualTo(new[] { "puls-tools", "puls-cook" }));

    /// <summary>The eval's setup has a mode for each member of the enum, and no other.</summary>
    [Test]
    public void TheCookEvalsModes_AreExactlyTheEnum() =>
        Assert.That(CookSetup.Modes.Keys, Is.EquivalentTo(Enum.GetValues<CookMode>().Select(m => m.Wire())));

    /// <summary>One mode is Puls with the cook's tools, the other Puls with its cook to hand off to.</summary>
    [Test]
    public void TheModes_ArePulsWithTheCooksToolsAndPulsWithItsCook() =>
        Assert.That(CookSetup.Modes, Is.EqualTo(new Dictionary<string, IReadOnlyList<string>>
        {
            [CookMode.Tools.Wire()] = [PulsToolset.Web.Wire(), PulsToolset.Shops.Wire()],
            [CookMode.Cook.Wire()] = [PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire()],
        }));
}
