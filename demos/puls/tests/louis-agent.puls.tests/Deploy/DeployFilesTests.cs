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
        var ports = Compose.Select(line => line.Trim())
            .Where(line => line.StartsWith("- \"", StringComparison.Ordinal));

        Assert.That(ports, Is.Not.Empty);
        Assert.That(ports, Is.All.StartsWith("- \"${PRIVATE_IP}:"));
    }

    /// <summary>The Azure build, one file for both apps (F109.2: the demo's app beside Puls, split later).</summary>
    private static readonly string[] Build = File.ReadAllLines(
        Path.Combine(TestPaths.RepoRoot, "demos/puls/config/azure/main.bicep"));

    /// <summary>
    /// The second app has what the first has in the build: its own identity to read the vault, its own file share
    /// on the storage account, mounted on the environment under its own name, and the outputs the verbs read back.
    /// </summary>
    [TestCase("name: 'louis-agent-app-identity'")]
    [TestCase("name: 'louis-agent-workspace'")]
    [TestCase("shareName: 'louis-agent-workspace'")]
    [TestCase("name: 'louis-agent-vol'")]
    [TestCase("output codingAppIdentityId string")]
    public void TheBuild_GivesTheSecondApp_ItsOwnIdentityAndShare(string line) =>
        Assert.That(Build, Has.Some.Contains(line));

    /// <summary>The deploy script, one for both apps: every verb takes -App.</summary>
    private static readonly string Script = File.ReadAllText(
        Path.Combine(TestPaths.RepoRoot, "demos/puls/deploy/azure/deploy.ps1"));

    /// <summary>The second app's yaml, from Puls's, with what's its own.</summary>
    private static readonly string[] CodingApp = File.ReadAllLines(
        Path.Combine(TestPaths.RepoRoot, "demos/puls/deploy/azure/louis-agent-app.yaml"));

    /// <summary>The script knows both apps by name, and the second one's image, yaml and host.</summary>
    [TestCase("'puls-app', 'louis-agent-app'")]
    [TestCase("louis-agent-coding")]
    [TestCase("louis-agent-app.yaml")]
    [TestCase("louis-agent.dev")]
    public void TheScript_KnowsBothApps(string text) => Assert.That(Script, Does.Contain(text));

    /// <summary>
    /// The second app's yaml names its own app, image, identity placeholder, share, tenant, cookie domain and keys,
    /// and keeps the markers the script fills.
    /// </summary>
    [TestCase("name: louis-agent-app")]
    [TestCase("image: acrpulsza.azurecr.io/louis-agent-coding:TAG")]
    [TestCase("'APP_IDENTITY_ID': {}")]
    [TestCase("storageName: louis-agent-vol")]
    [TestCase("value: louis-agent")]
    [TestCase("value: louis-agent.dev")]
    [TestCase("secretRef: agent-api-key-louis-agent")]
    [TestCase("secretRef: admin-api-key-louis-agent")]
    [TestCase("# SECRETS")]
    [TestCase("# PROVIDERS")]
    public void TheSecondAppsYaml_NamesWhatIsItsOwn(string line) =>
        Assert.That(CodingApp.Select(l => l.Trim()), Does.Contain(line));

    /// <summary>An image by tag alone can change under us between two boots; a digest can't.</summary>
    [Test]
    public void EveryImage_IsPinnedByDigest()
    {
        var images = Compose.Select(line => line.Trim())
            .Where(line => line.StartsWith("image:", StringComparison.Ordinal));

        Assert.That(images, Is.Not.Empty);
        Assert.That(images, Is.All.Contains("@sha256:"));
    }
}
