using LouisAgent.Choices;
using LouisAgent.Core.Skills;
using LouisAgent.Packs.Calculator.Tools;

namespace LouisAgent.Packs.Calculator.Tests;

/// <summary>The pack is one toolset with its guide, and needs nothing set to load.</summary>
public class CalculatorPackTests
{
    /// <summary>Its name, its toolset and its guide are the pack's own, and the guide says what to route here.</summary>
    [Test]
    public void TheGuide_ComesFromThePack_NotFromCore()
    {
        var pack = new CalculatorPack();

        Assert.That(pack.Name, Is.EqualTo(CalculatorToolset.Calculator.Wire()));
        Assert.That(pack.Toolsets.Select(t => t.Name), Is.EqualTo(new[] { CalculatorToolset.Calculator.Wire() }));
        Assert.That(pack.Toolsets[0].GuideFile, Is.EqualTo("toolsets/calculator.md"));
        Assert.That(SkillFileSource.CoreFiles, Has.None.EqualTo("toolsets/calculator.md"), "core's don't hold it");
        Assert.That(SkillFileSource.ReadEmbedded("toolsets/calculator.md"),
            Does.Contain("Route here any sum").And.Not.Contain("## Skill:"));
    }

    /// <summary>No setting and no browser: it loads wherever it is named, and its sums can run side by side.</summary>
    [Test]
    public void ItNeedsNoSetting_AndRunsInParallel()
    {
        var toolset = new CalculatorPack().Toolsets[0];

        Assert.That(toolset.MissingSettings(null!), Is.Empty);
        Assert.That(toolset.ParallelSafe, Is.True);
        Assert.That(toolset.ToolTypes, Is.EqualTo(new[] { typeof(CalculatorTools) }));
    }
}
