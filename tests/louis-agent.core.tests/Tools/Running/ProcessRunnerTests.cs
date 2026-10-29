using LouisAgent.Core.Tools;

namespace LouisAgent.Core.Tests.Tools.Running;

/// <summary>
/// The process runner hands a child its input and brings back its exit code and output, whether or not the child
/// read the input (FX23: a script that exited first made the write throw "Broken pipe", and the run was reported as
/// failed with its output lost). Platform-neutral: <c>dotnet</c> is where the tests run, and <c>sort</c> reads its
/// input on Linux and on Windows alike.
/// </summary>
public class ProcessRunnerTests
{
    /// <summary>Long enough for a slow runner, short enough to fail a hang.</summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// A megabyte of input: more than the pipe holds, so a child that never reads it closes it first.
    /// </summary>
    private static readonly string Megabyte = new('x', 1 << 20);

    /// <summary>
    /// A child that exits without reading a megabyte of input still gives its exit code and its output, not −1 and
    /// "Broken pipe".
    /// </summary>
    [Test]
    public void AChildThatNeverReadsItsInput_KeepsItsExitCodeAndOutput()
    {
        var result = ProcessRunner.Run("dotnet", ["--version"], Path.GetTempPath(), Timeout, Megabyte);

        Assert.That(result.ExitCode, Is.EqualTo(0), result.Output);
        Assert.That(result.Output.Trim(), Does.Match(@"^\d+\.\d+"), "the version, not an error");
        Assert.That(result.TimedOut, Is.False);
    }

    /// <summary>A child that reads its input still gets all of it.</summary>
    [Test]
    public void AChildThatReadsItsInput_GetsIt()
    {
        var result = ProcessRunner.Run("sort", [], Path.GetTempPath(), Timeout, "b\na\n");

        Assert.That(result.ExitCode, Is.EqualTo(0), result.Output);
        Assert.That(result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()),
            Is.EqualTo(new[] { "a", "b" }));
    }
}
