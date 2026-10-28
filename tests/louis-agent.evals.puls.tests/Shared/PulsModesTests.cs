using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Puls.Shared;

namespace LouisAgent.Evals.Puls.Tests.Shared;

/// <summary>
/// A mode can hand a question on when it has the specialists toolset, whatever the mode is called. Both evals ask
/// this, so it lives in one place.
/// </summary>
public class PulsModesTests
{
    /// <summary>A mode with the specialists can hand on, even under a name that isn't the cook's.</summary>
    [Test]
    public void AModeWithTheSpecialists_CanHandOn_WhateverItsName() =>
        Assert.That(
            PulsModes.CanDelegate(new EvalMode("puls-cook-sonnet", PulsEvalRole.Cook.Wire(),
                [PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire()])), Is.True);

    /// <summary>A mode named for the cook, but without the specialists, can't.</summary>
    [Test]
    public void AModeNamedForTheCook_WithoutTheSpecialists_CannotHandOn()
    {
        Assert.That(
            PulsModes.CanDelegate(new EvalMode(CookMode.Cook.Wire(), PulsEvalRole.Cook.Wire(),
                [PulsToolset.Web.Wire(), PulsToolset.Shops.Wire()])), Is.False);
        Assert.That(
            PulsModes.CanDelegate(new EvalMode(CookMode.Tools.Wire(), PulsEvalRole.Cook.Wire(),
                [PulsToolset.Web.Wire(), PulsToolset.Shops.Wire()])), Is.False);
    }

    /// <summary>Minions alone aren't a hand-off: only the specialists are.</summary>
    [Test]
    public void MinionsAlone_CannotHandOn() =>
        Assert.That(
            PulsModes.CanDelegate(new EvalMode("minions-only", PulsEvalRole.Cook.Wire(),
                [PulsToolset.Minions.Wire(), PulsToolset.Web.Wire()])), Is.False);
}
