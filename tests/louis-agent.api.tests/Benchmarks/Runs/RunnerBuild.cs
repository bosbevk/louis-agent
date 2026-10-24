namespace LouisAgent.Api.Tests.Benchmarks.Runs;

/// <summary>The eval runner's built dll, beside this test's configuration: the project reference builds it with these tests (F18 step 4.3).</summary>
internal static class RunnerBuild
{
    public static string Dll
    {
        get
        {
            string repo = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
            string configuration = new DirectoryInfo(TestContext.CurrentContext.TestDirectory).Parent!.Name;
            return Path.Combine(repo, "evals", "louis-agent.evals.runner", "bin", configuration, "net10.0", "louis-agent.evals.runner.dll");
        }
    }
}
