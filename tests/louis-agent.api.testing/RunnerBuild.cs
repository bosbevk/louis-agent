namespace LouisAgent.Api.Testing;

/// <summary>The eval runner's built dll, beside this test's configuration: the project reference builds it with these tests (F18 step 4.3).</summary>
public static class RunnerBuild
{
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
