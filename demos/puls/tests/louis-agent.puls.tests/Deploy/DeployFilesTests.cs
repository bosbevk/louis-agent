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

    /// <summary>
    /// The script knows the one app it deploys since FX122 (Louis's option A: Puls is not deployed), and its image,
    /// yaml and host.
    /// </summary>
    [TestCase("[ValidateSet('louis-agent-app')]")]
    [TestCase("louis-agent-coding")]
    [TestCase("louis-agent-app.yaml")]
    [TestCase("louis-agent.dev")]
    public void TheScript_KnowsBothApps(string text) => Assert.That(Script, Does.Contain(text));

    /// <summary>
    /// The app's yaml names its own app, image, identity placeholder, share and keys, and keeps the markers the
    /// script fills; since FX122 it names no VM, Mongo or accounts.
    /// </summary>
    [TestCase("name: louis-agent-app")]
    [TestCase("image: acrpulsza.azurecr.io/louis-agent-coding:TAG")]
    [TestCase("'APP_IDENTITY_ID': {}")]
    [TestCase("storageName: louis-agent-vol")]
    [TestCase("secretRef: agent-api-key-louis-agent")]
    [TestCase("secretRef: admin-api-key-louis-agent")]
    [TestCase("# SECRETS")]
    [TestCase("# PROVIDERS")]
    public void TheSecondAppsYaml_NamesWhatIsItsOwn(string line) =>
        Assert.That(CodingApp.Select(l => l.Trim()), Does.Contain(line));

    /// <summary>
    /// A release numbers its revision from every revision the app has (FX123): a revision made outside the script
    /// carries Azure's own name (louis-agent-app--0000001), so the latest alone can't say which vNNN comes next and
    /// the release stopped; the highest vNNN among them all can.
    /// </summary>
    [TestCase("containerapp revision list -n $App -g $ResourceGroup --all --query '[].name' -o tsv")]
    [TestCase("Measure-Object -Maximum")]
    public void TheRelease_NumbersFromEveryRevision(string text) => Assert.That(Script, Does.Contain(text));

    /// <summary>
    /// The site runs alone (FX122, Louis's option A, "we do not need dbs"): its yaml points at no VM, no Mongo and no
    /// accounts database, so the VM and the SQL server can go without breaking a release.
    /// </summary>
    [TestCase("VM_IP")]
    [TestCase("EVALS_MONGO_URL")]
    [TestCase("ACCOUNTS_SQL_URL")]
    [TestCase("SEARXNG_URL")]
    [TestCase("BROWSER_URL")]
    public void TheAppsYaml_NeedsNoVmAndNoDatabase(string text) =>
        Assert.That(string.Join("\n", CodingApp), Does.Not.Contain(text));

    /// <summary>
    /// The guide's mail key (FX87): the script copies RESEND_API_KEY from the app's .env.secrets into the vault, and
    /// the release names it in the app's secrets and env only when the vault holds it, so no hand edit of the yaml.
    /// </summary>
    [TestCase("RESEND_API_KEY = 'resend-api-key'")]
    [TestCase("$AppInfo.WhenSet")]
    [TestCase("'          # WHEN SET'")]
    public void TheScript_CarriesTheMailKey_OnlyOnceTheVaultHoldsIt(string text) =>
        Assert.That(Script, Does.Contain(text));

    /// <summary>
    /// The yaml keeps the marker the release fills and never names the mail key itself: a secretRef the vault lacks
    /// fails the whole release (FX86).
    /// </summary>
    [Test]
    public void TheSecondAppsYaml_LeavesTheMailKey_ToTheRelease()
    {
        var lines = CodingApp.Select(l => l.Trim()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(lines, Does.Contain("# WHEN SET"));
            Assert.That(lines, Has.None.EqualTo("secretRef: resend-api-key"));
        });
    }

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
