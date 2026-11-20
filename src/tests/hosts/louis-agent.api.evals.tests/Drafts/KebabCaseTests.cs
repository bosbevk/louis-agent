using LouisAgent.Api.Evals.Drafts;

namespace LouisAgent.Api.Evals.Tests.Evals.Drafts;

/// <summary>
/// A name a person typed becomes part of a file's name only when it is kebab-case, as the eval folders' scenario ids
/// and known-bad reasons are, and not too long.
/// </summary>
public class KebabCaseTests
{
    /// <summary>Lowercase letters and digits joined by single dashes are taken.</summary>
    [TestCase("cook-af")]
    [TestCase("no-route")]
    [TestCase("cook-af-6")]
    [TestCase("x")]
    public void KebabCase_IsTaken(string name) => Assert.That(KebabCase.Is(name), Is.True);

    /// <summary>Anything else, or nothing, or a name too long for a file, is not.</summary>
    [TestCase(null)]
    [TestCase("")]
    [TestCase("Cook-af")]
    [TestCase("cook af")]
    [TestCase("cook_af")]
    [TestCase("-cook")]
    [TestCase("cook-")]
    [TestCase("cook--af")]
    [TestCase("../cook")]
    [TestCase("kook-ê")]
    public void AnythingElse_IsNot(string? name) => Assert.That(KebabCase.Is(name), Is.False);

    /// <summary>A name longer than the limit is not taken, however neat.</summary>
    [Test]
    public void ANameTooLong_IsNot() => Assert.That(KebabCase.Is(new string('a', KebabCase.MaxLength + 1)), Is.False);
}
