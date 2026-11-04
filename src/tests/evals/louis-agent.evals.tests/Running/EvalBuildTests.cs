using System.Reflection;
using LouisAgent.Evals.Running;

namespace LouisAgent.Evals.Tests.Running;

/// <summary>
/// Where a run's commit comes from: the commit the build stamped on the harness, and "unknown" when it stamped none
/// or an empty one (a Docker build given no <c>EVAL_COMMIT</c>, or an empty one).
/// </summary>
public class EvalBuildTests
{
    /// <summary>A stamped commit is the run's commit, without the spaces a shell may leave around it.</summary>
    [TestCase("45a98b3", "45a98b3")]
    [TestCase(" 45a98b3\n", "45a98b3")]
    public void AStampedCommit_IsTheCommit(string stamped, string commit) =>
        Assert.That(EvalBuild.CommitOf(stamped), Is.EqualTo(commit));

    /// <summary>No stamp, or an empty one, is "unknown", never an empty commit.</summary>
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void NoStamp_OrAnEmptyOne_IsUnknown(string? stamped) =>
        Assert.That(EvalBuild.CommitOf(stamped), Is.EqualTo("unknown"));

    /// <summary>The harness's own commit is the one its project stamped.</summary>
    [Test]
    public void TheHarnessCommit_IsWhatTheProjectStamped()
    {
        string? stamped = typeof(EvalBuild).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "Commit").Value;

        Assert.That(EvalBuild.Commit, Is.EqualTo(EvalBuild.CommitOf(stamped)));
    }
}
