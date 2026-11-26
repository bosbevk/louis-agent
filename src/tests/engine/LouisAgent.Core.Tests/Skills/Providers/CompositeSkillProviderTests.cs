using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Providers;

namespace LouisAgent.Core.Tests.Skills.Providers;

/// <summary>
/// Several prompt sources read as one: their text in the order they were added, each followed by a blank line, a blank
/// or missing one left out.
/// </summary>
public class CompositeSkillProviderTests
{
    /// <summary>With no source there is no text.</summary>
    [Test]
    public void NoSources_GiveNoText() => Assert.That(new CompositeSkillProvider().Documentation, Is.Empty);

    /// <summary>The sources' text comes in the order they were given, trimmed, a blank line after each.</summary>
    [Test]
    public void TheSources_ComeInOrder_EachWithABlankLineAfter()
    {
        var provider = new CompositeSkillProvider(new Text("  # First \n"), new Text("# Second"));

        Assert.That(provider.Documentation.ReplaceLineEndings("\n"), Is.EqualTo("# First\n\n# Second\n\n"));
    }

    /// <summary>A blank source and a null one are left out.</summary>
    [Test]
    public void ABlankOrNullSource_IsLeftOut()
    {
        var provider = new CompositeSkillProvider(new Text("   "), null, new Text("# Only"));
        provider.AddProvider(null);

        Assert.That(provider.Documentation.ReplaceLineEndings("\n"), Is.EqualTo("# Only\n\n"));
    }

    /// <summary>A source added after the text was read is in the next read.</summary>
    [Test]
    public void ASourceAddedLater_IsInTheNextRead()
    {
        var provider = new CompositeSkillProvider(new Text("# First"));
        _ = provider.Documentation;

        provider.AddProvider(new Text("# Later"));

        Assert.That(provider.Documentation, Does.Contain("# First").And.Contain("# Later"));
    }

    /// <summary>A source that is only its text.</summary>
    private sealed record Text(string Documentation) : ISkillProvider;
}
