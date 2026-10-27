using System.Diagnostics;

namespace LouisAgent.Core.Tests;

// F18 step 7 (the plan, line 33): the repo is C#. Python lives in the images because skills run it (their `Execution:` fences), and the agent writes some under Skills/, but no `.py` file
// is tracked anywhere else: the harness's Python checks, their tests and CI's Python step are gone, and this keeps them gone. Read from git, not the folder, so an ignored scratch file is
// not a failure and a tracked one never hides. If git can't be found, it asks through Outside.Tool: skipped on a developer's machine, a failure where INTEGRATION_REQUIRED is set (CI), so the rule never passes quietly there.
[Category("Integration")]
public class NoPythonTests
{
    [Test]
    public void NoPythonFile_IsTrackedOutsideSkills()
    {
        var tracked = TrackedFiles();

        var python = tracked.Where(f => f.EndsWith(".py", StringComparison.OrdinalIgnoreCase) && !f.StartsWith("Skills/", StringComparison.Ordinal)).Order(StringComparer.Ordinal).ToList();

        Assert.That(python, Is.Empty, "The repo is C#: a skill's Python lives under Skills/ and nothing else is tracked (F18 step 7):\n  " + string.Join("\n  ", python));
    }

    private static List<string> TrackedFiles()
    {
        var start = new ProcessStartInfo("git") { WorkingDirectory = TestPaths.RepoRoot, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add("ls-files");
        start.ArgumentList.Add("-z");
        try
        {
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            if (process.ExitCode != 0) Assert.Fail($"git ls-files failed (exit {process.ExitCode}), so no file could be checked: {errors.Result.Trim()}");
            return [.. output.Result.Split('\0', StringSplitOptions.RemoveEmptyEntries)];
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Outside.Tool(false, $"git isn't available here ({ex.Message}), so no file could be checked");
            return [];
        }
    }
}
