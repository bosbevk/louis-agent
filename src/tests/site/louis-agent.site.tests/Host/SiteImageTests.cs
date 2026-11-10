namespace LouisAgent.Site.Tests.Host;

/// <summary>
/// The framework's site runs from an image of its own (F124): its host, the docs the guide reads, laid out as the repo
/// lays them so their links hold, and the price tables its ledger reads; on the PC, on 127.0.0.1:5080 only, where the
/// review copy has always been.
/// </summary>
/// <remarks>Plain string checks on the files' lines, no regex (ADR 0057).</remarks>
public class SiteImageTests
{
    private static string[] Lines(string path) =>
        File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, path)).Select(line => line.Trim()).ToArray();

    /// <summary>The image publishes the site's host, carries the docs and the price tables, and starts it.</summary>
    [TestCase(
        "RUN dotnet publish \"src/site/louis-agent.site-api/louis-agent.site-api.csproj\" -c Release -o /app/publish")]
    [TestCase("COPY --from=build /src/src/docs/ /app/docs/src/docs/")]
    [TestCase("COPY --from=build /src/packs/docs/ /app/docs/packs/docs/")]
    [TestCase("RUN rm -rf /app/docs/src/docs/films")]
    [TestCase("ENV DOCS_ROOT=/app/docs")]
    [TestCase("COPY --from=build /src/src/site/config/prices.json /src/src/site/config/rates.json /app/config/")]
    [TestCase("ENV PRICES_FILE=/app/config/prices.json")]
    [TestCase("ENTRYPOINT [\"dotnet\", \"louis-agent.site-api.dll\"]")]
    public void TheImage_CarriesTheSite(string line) =>
        Assert.That(Lines("src/site/docker/Dockerfile"), Does.Contain(line));

    /// <summary>On the PC the site answers on 127.0.0.1:5080, the review copy's address, and nowhere else.</summary>
    [Test]
    public void TheSite_IsTheReviewCopy_OnThisPCOnly() =>
        Assert.That(Lines("src/site/docker/docker-compose.yml"),
            Does.Contain("- \"127.0.0.1:${SITE_PORT:-5080}:8080\""));

    /// <summary>
    /// On the PC the site shows costs in rand, as in Azure (F127.1, Louis: "display in rands"): the same culture, so
    /// the guide's line under an answer reads R, not the dollars it is charged in.
    /// </summary>
    [Test]
    public void TheSite_ShowsRand_OnThePCAsInAzure()
    {
        string[] azure = Lines("src/site/deploy/azure/louis-agent-app.yaml");
        int culture = Array.IndexOf(azure, "- name: HOST_CULTURE");

        Assert.Multiple(() =>
        {
            Assert.That(azure[culture + 1], Is.EqualTo("value: en-ZA"));
            Assert.That(Lines("src/site/docker/docker-compose.yml"), Does.Contain("HOST_CULTURE: en-ZA"));
        });
    }

    /// <summary>The coding agent's API moves aside to 5082, so the two run together (Louis's pick 2A).</summary>
    [Test]
    public void TheCodingApi_MovesTo5082() =>
        Assert.That(Lines("demos/louis-agent/docker/docker-compose.yml"),
            Does.Contain("- \"127.0.0.1:${API_PORT:-5082}:8080\""));
}
