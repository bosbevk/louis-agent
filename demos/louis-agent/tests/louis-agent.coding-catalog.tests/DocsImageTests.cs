namespace LouisAgent.Coding.Tests;

/// <summary>
/// The expert's docs travel with the app (F109.3 step 4): the image carries the repo's docs laid out as the repo lays
/// them, so their links hold, with the films left out, and every way the app runs names the docs root.
/// </summary>
/// <remarks>Plain string checks on the files' lines, no regex (ADR 0057).</remarks>
public class DocsImageTests
{
    private static string[] Lines(string path) =>
        File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, path)).Select(line => line.Trim()).ToArray();

    /// <summary>The image copies each docs folder to its repo path under /app/docs, drops the films, and names the root.</summary>
    [TestCase("COPY --from=build /src/src/docs/ /app/docs/src/docs/")]
    [TestCase("COPY --from=build /src/demos/louis-agent/docs/ /app/docs/demos/louis-agent/docs/")]
    [TestCase("COPY --from=build /src/demos/puls/docs/ /app/docs/demos/puls/docs/")]
    [TestCase("COPY --from=build /src/packs/docs/ /app/docs/packs/docs/")]
    [TestCase("RUN rm -rf /app/docs/demos/louis-agent/docs/films")]
    [TestCase("ENV DOCS_ROOT=/app/docs")]
    public void TheImage_CarriesTheDocs(string line) =>
        Assert.That(Lines("demos/louis-agent/docker/images/Dockerfile.api"), Does.Contain(line));

    /// <summary>On the PC the repo is the docs root, mounted as the workspace; in Azure the image's copy.</summary>
    [Test]
    public void EveryWayTheAppRuns_NamesTheDocsRoot()
    {
        Assert.That(Lines("demos/louis-agent/docker/docker-compose.yml"), Does.Contain("DOCS_ROOT: /workspace"));
        Assert.That(Lines("demos/puls/deploy/azure/louis-agent-app.yaml"), Does.Contain("value: /app/docs"));
    }
}
