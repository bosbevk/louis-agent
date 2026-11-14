using LouisAgent.Diagrams.Svg.Theme;
using LouisAgent.Diagrams.Svg.Theme.Looks;

namespace LouisAgent.Diagrams.Tests.Svg.Theme.Looks;

/// <summary>
/// What a flow's look adds to the shared one (F134.8), in the others' look since F134.12: a shape's thin outline
/// with no glow, no frame of its own for a group, and a key under the accent's rule.
/// </summary>
public class FlowStyleTests
{
    private static readonly string Own = FlowStyle.Rules;

    /// <summary>The flow's style has a rule for each part that is a flow's own.</summary>
    [TestCase(".flow-name")]
    [TestCase(".flow-icon")]
    [TestCase(".flow-shape")]
    [TestCase(".flow-chip")]
    [TestCase(".flow-word")]
    [TestCase(".flow-rule")]
    [TestCase(".flow-means")]
    public void TheFlowsStyle_HasARuleForEachOfItsOwnParts(string rule) => Assert.That(Own, Does.Contain(rule + " "));

    /// <summary>
    /// A shape that means something has a thin outline, grey until its tone colours it, and no glow and no shadow:
    /// the other kinds have none.
    /// </summary>
    [Test]
    public void AShape_HasAThinOutline_AndNoGlow() => Assert.Multiple(() =>
    {
        Assert.That(Own, Does.Contain(".flow-shape { stroke: var(--quiet); stroke-width: 1.25;"));
        Assert.That(Own, Does.Not.Contain("drop-shadow").And.Not.Contain("--flow-glow"));
    });

    /// <summary>A group's frame is a panel like a box, so the flow's style has no rule for one.</summary>
    [Test]
    public void AGroupsFrame_IsAPanel_SoTheStyleHasNoRuleForIt() => Assert.That(Own, Does.Not.Contain(".flow-group"));

    /// <summary>The rule over the key is filled with the accent's edge, as the other kinds' are.</summary>
    [Test]
    public void TheKeysRule_IsTheAccentsEdge() =>
        Assert.That(Own, Does.Contain($".flow-rule {{ fill: url(#{MarqueeLook.EdgePaint});"));

    /// <summary>
    /// Each rule is written in one place: the flow's own style names nothing of the shared looks, and the same
    /// rules serve every theme, since their colours are the theme's own variables.
    /// </summary>
    [Test]
    public void EachRule_IsWrittenInOnePlace() =>
        Assert.That(Own, Does.Not.Contain(".pic-").And.Not.Contain(".mrq-").And.Not.Contain(".cmp-"));
}
