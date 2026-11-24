namespace LouisAgent.Coding.Tests;

/// <summary>
/// The demo's API host takes the evals as the two sites' hosts do (F126.5.3): their services and routes behind the
/// admin key, and a data folder where their runs and recordings are kept, so the triage team is measured on the host
/// that runs it. The routes themselves are <c>LouisAgent.Api.Evals</c>'s, tested there.
/// </summary>
/// <remarks>Plain string checks on the files' lines, no regex (ADR 0057).</remarks>
public class DemoEvalsTests
{
    /// <summary>A file's lines, each trimmed.</summary>
    private static string[] Lines(string path) =>
        File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, path)).Select(line => line.Trim()).ToArray();

    /// <summary>The program adds the evals' services and maps their routes with the admin's key.</summary>
    [Test]
    public void TheProgram_TakesTheEvals_BehindTheAdminKey()
    {
        string program = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "demos/louis-agent/hosts/LouisAgent.CodingApi/Program.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(program, Does.Contain("AddLouisAgentEvals(host"));
            Assert.That(program, Does.Contain("EvalsOptions.FromEnvironment()"));
            Assert.That(program, Does.Contain("AdminOptions.FromEnvironment().AdminKey"));
            Assert.That(program, Does.Contain("MapLouisAgentEvals(adminKey)"));
        });
    }

    /// <summary>
    /// The compose service keeps the evals' runs and recordings under the demo's git-ignored data, and names itself as
    /// a peer, so the triage manager can ask its roles on the same host (Louis's pick, "Self as the demo's peer").
    /// </summary>
    [TestCase("SITE_DATA_DIRECTORY: /workspace/demos/louis-agent/data")]
    [TestCase("REPLAY_DIRECTORY: /workspace/demos/louis-agent/data/recordings")]
    [TestCase("AGENT_PEERS: ${AGENT_PEERS:-self=http://localhost:8080}")]
    public void TheService_KeepsTheEvalsData(string line) =>
        Assert.That(Lines("demos/louis-agent/docker/docker-compose.yml"), Does.Contain(line));

    /// <summary>The run profile keeps them in the same folder, so a debug run sees what the container does.</summary>
    [TestCase("\"SITE_DATA_DIRECTORY\": \"../../data\",")]
    [TestCase("\"REPLAY_DIRECTORY\": \"../../data/recordings\",")]
    [TestCase("\"AGENT_PEERS\": \"self=http://127.0.0.1:5081\"")]
    public void TheRunProfile_KeepsTheEvalsData(string line) =>
        Assert.That(Lines("demos/louis-agent/hosts/LouisAgent.CodingApi/Properties/launchSettings.json"),
            Does.Contain(line));
}
