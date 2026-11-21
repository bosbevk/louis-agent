namespace LouisAgent.SiteFramework.Tests.Host;

/// <summary>
/// The framework's image carries the docs the framework Orbi reads (F164.2 step 2), laid out as the repo lays them so
/// their links hold, and its host is given the settings the public Orbi's is: the model, the door open to its agent,
/// and its words paced once, by the server; on the PC as in its run profile.
/// </summary>
/// <remarks>Plain string checks on the files' lines, no regex (ADR 0057).</remarks>
public class FrameworkImageTests
{
    /// <summary>A file's lines, each trimmed.</summary>
    private static string[] Lines(string path) =>
        File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, path)).Select(line => line.Trim()).ToArray();

    /// <summary>The image carries the docs, without the films, and says where they are.</summary>
    [TestCase("COPY --from=build /src/README.md /src/ROADMAP.md /src/TODO.md /app/docs/")]
    [TestCase("COPY --from=build /src/src/docs/ /app/docs/src/docs/")]
    [TestCase("COPY --from=build /src/demos/louis-agent/docs/ /app/docs/demos/louis-agent/docs/")]
    [TestCase("COPY --from=build /src/demos/puls/docs/ /app/docs/demos/puls/docs/")]
    [TestCase("COPY --from=build /src/packs/docs/ /app/docs/packs/docs/")]
    [TestCase("RUN rm -rf /app/docs/src/docs/films")]
    [TestCase("ENV DOCS_ROOT=/app/docs")]
    public void TheImage_CarriesTheDocs(string line) =>
        Assert.That(Lines("src/site/docker/Dockerfile.framework"), Does.Contain(line));

    /// <summary>
    /// The compose service gives the host the model, the open door to the framework Orbi at the cap the live site
    /// has, 20 sessions a day (F164.3, Louis's pick A of three), and pacing.
    /// </summary>
    [TestCase("- path: ../config/.env.anthropic")]
    [TestCase("OPEN_IDENTITIES: louis-agent-framework")]
    [TestCase("OPEN_SESSIONS_PER_DAY: \"20\"")]
    [TestCase("STREAM_PACE: 'on'")]
    public void TheService_RunsTheFrameworkOrbi(string line)
    {
        string[] compose = Lines("src/site/docker/docker-compose.yml");
        int framework = Array.IndexOf(compose, "framework:");

        Assert.That(compose.Skip(framework), Does.Contain(line));
    }

    /// <summary>
    /// The run profile reads the repo's docs and opens the same door, at the same cap, so a debug run sees what the
    /// image does.
    /// </summary>
    [TestCase("\"DOCS_ROOT\": \"../../..\",")]
    [TestCase("\"OPEN_IDENTITIES\": \"louis-agent-framework\",")]
    [TestCase("\"OPEN_SESSIONS_PER_DAY\": \"20\"")]
    public void TheRunProfile_ReadsTheDocs_AndOpensTheDoor(string line) =>
        Assert.That(Lines("src/site/LouisAgent.SiteFrameworkApi/Properties/launchSettings.json"),
            Does.Contain(line));
}
