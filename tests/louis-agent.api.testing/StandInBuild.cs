namespace LouisAgent.Api.Testing;

/// <summary>
/// Finds the stand-in we start instead of the real eval runner, built alongside these tests the same way as
/// <see cref="RunnerBuild"/>.
/// </summary>
public static class StandInBuild
{
    /// <summary>Where the stand-in's dll sits, in the same build folders as this test.</summary>
    public static string Dll
    {
        get
        {
            // tests/{project}/bin/{configuration}/{framework}: the stand-in is built in the same two folders.
            var build = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            return Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.api.standin", "bin", build.Parent!.Name,
                build.Name, "louis-agent.api.standin.dll");
        }
    }
}
