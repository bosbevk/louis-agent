using LouisAgent.Core.Tools.Git.Running;

namespace LouisAgent.Core.Tests.Tools.Git.Running;

// FX19, by rule (S, CL15): a value from the model that names something (a ref, branch, tag, remote, URL, path or config
// key) is refused before git runs when git would read it as an option; a branch that is really a refspec is refused
// too (review of PR #66), since +main forces a push, :main deletes the remote's branch and a:b pushes to another ref.
public class GitArgumentsTests
{
    [TestCase("-x")]
    [TestCase("--force")]
    [TestCase("  -n")]
    public void AValueThatStartsWithADash_IsRefused_NamingTheParameter(string value) =>
        Assert.That(GitArguments.Refused("branchName", value), Does.Contain("branchName").And.Contain(value.Trim()));

    [TestCase("main")]
    [TestCase("feature/x-1")]
    [TestCase("HEAD~1")]
    [TestCase("")]
    public void AnyOtherValue_Passes(string value) =>
        Assert.That(GitArguments.Refused("commitRef", value), Is.Null);

    [TestCase("+main")]
    [TestCase(":main")]
    [TestCase("main:other")]
    [TestCase("-f")]
    public void ABranchThatIsARefspec_OrAnOption_IsRefused(string branch) =>
        Assert.That(GitArguments.RefusedRefspec("branch", branch), Is.Not.Null);

    [TestCase("main")]
    [TestCase("")]
    public void ABranchNameAlone_OrNothing_Passes(string branch) =>
        Assert.That(GitArguments.RefusedRefspec("branch", branch), Is.Null);

    [Test]
    public void TheModelsValues_ComeAfterTheEndOfOptions() =>
        Assert.That(GitArguments.EndOfOptions, Is.EqualTo("--end-of-options"));
}
