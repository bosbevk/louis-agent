namespace LouisAgent.Api.Testing;

/// <summary>
/// Finds the test runner we built alongside these tests, so a test can start it: the framework's eval runner with the
/// general checks and the template eval, no product's (F166.1). The project reference builds it in the same
/// configuration and framework as the tests.
/// </summary>
public static class RunnerBuild
{
    /// <summary>The test runner's project, its folder's name and its dll's.</summary>
    private const string Project = "LouisAgent.Api.TestRunner";

    /// <summary>Where the runner's dll sits, in the same build folders as this test.</summary>
    public static string Dll
    {
        get
        {
            // {project}/bin/{configuration}/{framework}: the runner is built in the same two folders.
            var build = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            return Path.Combine(TestPaths.RepoRoot, "src/tests/hosts", Project, "bin", build.Parent!.Name, build.Name,
                $"{Project}.dll");
        }
    }
}
