namespace LouisAgent.Api.Testing;

/// <summary>
/// Finds the eval runner we built alongside these tests, so a test can start it. The project reference builds it in
/// the same configuration and framework as the tests.
/// </summary>
public static class RunnerBuild
{
    /// <summary>Where the runner's dll sits, in the same build folders as this test.</summary>
    public static string Dll
    {
        get
        {
            // tests/{project}/bin/{configuration}/{framework}: the runner is built in the same two folders.
            var build = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            return Path.Combine(TestPaths.RepoRoot, "evals", "louis-agent.evals.runner", "bin", build.Parent!.Name,
                build.Name, "louis-agent.evals.runner.dll");
        }
    }
}
