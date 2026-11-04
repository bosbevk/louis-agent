using System.Text.RegularExpressions;
using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Protocol.Tests.Kinds;

/// <summary>
/// Every word of a kind on the wire is lower-case kebab-case (card C), and each one parses back to its member and is
/// written by it.
/// </summary>
public partial class KindWireNamesTests
{
    /// <summary>The grader kinds.</summary>
    [Test]
    public void GraderKinds() => AssertWireNames<CheckKind>();

    /// <summary>A grade's outcomes.</summary>
    [Test]
    public void GradeOutcomes() => AssertWireNames<GradeOutcome>();

    /// <summary>A conversation's outcomes.</summary>
    [Test]
    public void TrialOutcomes() => AssertWireNames<TrialOutcome>();

    /// <summary>The parts of a run a model works for.</summary>
    [Test]
    public void ModelLevels() => AssertWireNames<ModelLevel>();

    /// <summary>What ends a turn early.</summary>
    [Test]
    public void TurnFailures() => AssertWireNames<TurnFailure>();

    /// <summary>The scenarios a run asks for.</summary>
    [Test]
    public void RunSplits() => AssertWireNames<RunSplit>();

    /// <summary>
    /// Each member has a kebab-case wire name, it parses back to the member, and the member writes it.
    /// </summary>
    private static void AssertWireNames<T>() where T : struct, Enum
    {
        Assert.Multiple(() =>
        {
            foreach (T member in Enum.GetValues<T>())
            {
                string wire = member.Wire();
                Assert.That(wire, Does.Match(KebabCase()), $"{typeof(T).Name}.{member}");
                Assert.That(SettingChoice.TryParse(wire, out T? parsed) ? parsed : null, Is.EqualTo(member));
            }
        });
    }

    /// <summary>Lower-case words joined by dashes.</summary>
    [GeneratedRegex("^[a-z]+(-[a-z]+)*$")]
    private static partial Regex KebabCase();
}
