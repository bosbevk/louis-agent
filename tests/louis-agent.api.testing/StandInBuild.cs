namespace LouisAgent.Api.Testing;

/// <summary>The stand-in for the eval runner's built dll (F18 step 7.4), beside this test's configuration like <see cref="RunnerBuild"/>: the project reference builds it with these tests.</summary>
public static class StandInBuild
{
    public static string Dll
    {
        get
        {
            var build = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            return Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.api.standin", "bin", build.Parent!.Name,
                build.Name, "louis-agent.api.standin.dll");
        }
    }
}
