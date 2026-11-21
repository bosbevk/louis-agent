using LouisAgent.Orchestrator.Tools;

namespace LouisAgent.Orchestrator.Tests.Tools;

/// <summary>
/// What the model may pass to git: only a commit hash and a branch name that can't be read as an option.
/// </summary>
public class ServiceRepositoryTests
{
    /// <summary>A hash is 7 to 40 hex characters, nothing else.</summary>
    [TestCase("abc1234", true)]
    [TestCase("ABCDEF0123456789abcdef0123456789abcdef01", true)]
    [TestCase("abc123", false)]
    [TestCase("ABCDEF0123456789abcdef0123456789abcdef012", false)]
    [TestCase("--all", false)]
    [TestCase("HEAD~1a", false)]
    public void IsCommitHash(string commit, bool expected) =>
        Assert.That(ServiceRepository.IsCommitHash(commit), Is.EqualTo(expected));

    /// <summary>A branch is up to 100 letters, digits and ._/-, and never starts with a dash.</summary>
    [TestCase("fix/order-total-abc123", true)]
    [TestCase("release_1.2", true)]
    [TestCase("", false)]
    [TestCase("-D", false)]
    [TestCase("fix x", false)]
    [TestCase("fix;rm", false)]
    public void IsBranchName(string branch, bool expected) =>
        Assert.That(ServiceRepository.IsBranchName(branch), Is.EqualTo(expected));

    /// <summary>A name longer than 100 characters is refused.</summary>
    [Test]
    public void IsBranchName_RefusesALongName() =>
        Assert.That(ServiceRepository.IsBranchName(new string('a', 101)), Is.False);
}
