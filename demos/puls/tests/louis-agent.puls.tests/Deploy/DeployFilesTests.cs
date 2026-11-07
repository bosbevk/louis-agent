namespace LouisAgent.Puls.Tests.Deploy;

/// <summary>
/// Holds the VM's compose (F61.4) to what the plan promises: Mongo, the browser and SearXNG run on the private network
/// only, so a port never binds every address, and every image is pinned by digest, so a rebuilt VM runs what the PC's
/// stack ran and benchmark runs compare.
/// </summary>
/// <remarks>Plain string checks on the file's lines, no regex (ADR 0057).</remarks>
public class DeployFilesTests
{
    private static readonly string[] Compose = File.ReadAllLines(
        Path.Combine(TestPaths.RepoRoot, "demos/puls/deploy/azure/vm/docker-compose.services.yml"));

    /// <summary>The three services the app reaches on the VM, by the names its addresses use.</summary>
    [TestCase("mongo:")]
    [TestCase("browser:")]
    [TestCase("searxng:")]
    public void TheVm_RunsTheService(string service) =>
        Assert.That(Compose.Select(line => line.Trim()), Does.Contain(service));

    /// <summary>A port bound to every address would be open to whatever reaches the VM; the private IP only.</summary>
    [Test]
    public void EveryPort_BindsThePrivateAddress()
    {
        var ports = Compose.Select(line => line.Trim()).Where(line => line.StartsWith("- \"", StringComparison.Ordinal));

        Assert.That(ports, Is.Not.Empty);
        Assert.That(ports, Is.All.StartsWith("- \"${PRIVATE_IP}:"));
    }

    /// <summary>An image by tag alone can change under us between two boots; a digest can't.</summary>
    [Test]
    public void EveryImage_IsPinnedByDigest()
    {
        var images = Compose.Select(line => line.Trim()).Where(line => line.StartsWith("image:", StringComparison.Ordinal));

        Assert.That(images, Is.Not.Empty);
        Assert.That(images, Is.All.Contains("@sha256:"));
    }
}
