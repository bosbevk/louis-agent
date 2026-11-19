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

    /// <summary>The framework's site's yaml (F124: it moved to src/site with the site), with what's its own.</summary>
    private static readonly string[] CodingApp = File.ReadAllLines(
        Path.Combine(TestPaths.RepoRoot, "src/site/deploy/azure/louis-agent-app.yaml"));

    /// <summary>
    /// The script knows its two apps: louis-agent.dev since F124, with its image, Dockerfile, yaml and host, and the
    /// framework's site since F164.3.
    /// </summary>
    [TestCase("[ValidateSet('louis-agent-app', 'louis-agent-framework-app')]")]
    [TestCase("Image = 'louis-agent-site'")]
    [TestCase("Dockerfile = 'src\\site\\docker\\Dockerfile'")]
    [TestCase("Yaml = 'src\\site\\deploy\\azure\\louis-agent-app.yaml'")]
    [TestCase("louis-agent.dev")]
    public void TheScript_KnowsBothApps(string text) => Assert.That(Script, Does.Contain(text));

    /// <summary>
    /// The framework's site's entry in the script's table of apps (F164.3), from its own line to the brace that closes
    /// it.
    /// </summary>
    private static string FrameworkEntry()
    {
        int start = Script.IndexOf("'louis-agent-framework-app' = @{", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), "the table has no entry for louis-agent-framework-app");
        int end = Script.IndexOf("\n    }", start, StringComparison.Ordinal);
        return Script[start..end];
    }

    /// <summary>
    /// The framework's app is built from its own image and yaml, answers at its subdomain, and keeps its own keys in
    /// a file and under a suffix of its own, while it reads the vault with louis-agent.dev's identity and logs to its
    /// share (F164.3, Louis's pick A).
    /// </summary>
    [TestCase("Image = 'louis-agent-site-framework'")]
    [TestCase("Dockerfile = 'src\\site\\docker\\Dockerfile.framework'")]
    [TestCase("Yaml = 'src\\site\\deploy\\azure\\louis-agent-framework-app.yaml'")]
    [TestCase("Config = 'src\\site\\config'")]
    [TestCase("SecretsFile = '.env.secrets.framework'")]
    [TestCase("IdentityOutput = 'codingAppIdentityId'")]
    [TestCase("Share = 'louis-agent-workspace'")]
    [TestCase("Hostname = 'framework.louis-agent.dev'")]
    [TestCase("Suffix = '-framework'")]
    public void TheFrameworksEntry_NamesWhatIsItsOwn_AndWhatItShares(string text) =>
        Assert.That(FrameworkEntry(), Does.Contain(text));

    /// <summary>
    /// The domain verb tells an apex from a subdomain by its parts (F164.3): louis-agent.dev has two and takes the
    /// apex's A record; framework.louis-agent.dev has three and takes a CNAME and an asuid TXT at its own first label,
    /// never louis-agent.dev's own A record.
    /// </summary>
    [TestCase("$apex = $Hostname.Split('.').Count -eq 2")]
    [TestCase("$label = $Hostname.Split('.')[0]")]
    [TestCase("Write-Host \"  CNAME  $label")]
    [TestCase("Write-Host \"  TXT    asuid.$label")]
    public void TheDomainVerb_GivesASubdomainItsOwnRecords(string text) => Assert.That(Script, Does.Contain(text));

    /// <summary>The old rule, which took any name but www for an apex, is gone.</summary>
    [Test]
    public void TheDomainVerb_NoLongerTakesEveryNameButWwwForAnApex() =>
        Assert.That(Script, Does.Not.Contain("$Hostname -notmatch '^www\\.'"));

    /// <summary>
    /// The secrets each app reads come from its own file, so the two apps' keys can differ though they share a
    /// config folder.
    /// </summary>
    [TestCase("SecretsFile = '.env.secrets';")]
    [TestCase("Join-Path $AppConfig $AppInfo.SecretsFile")]
    public void EachApp_ReadsItsKeys_FromItsOwnFile(string text) => Assert.That(Script, Does.Contain(text));

    /// <summary>
    /// The framework's keys file is git-ignored, as every other secrets file is, and its example says what to fill
    /// in.
    /// </summary>
    [Test]
    public void TheFrameworksKeys_StayOutOfGit_AndHaveAnExample()
    {
        var ignored = File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, ".gitignore"));
        string example = Path.Combine(TestPaths.RepoRoot, "src/site/config/.env.secrets.framework.example");

        Assert.Multiple(() =>
        {
            Assert.That(ignored, Does.Contain("**/config/.env.secrets.framework"));
            Assert.That(File.Exists(example), "src/site/config/.env.secrets.framework.example");
        });
        Assert.That(File.ReadAllLines(example), Does.Contain("AGENT_API_KEY=").And.Contain("ADMIN_API_KEY="));
    }

    /// <summary>
    /// The app's yaml names its own app, image, identity placeholder, share and keys, and keeps the markers the
    /// script fills; since FX122 it names no VM, Mongo or accounts, and since F124 it runs the site's image.
    /// </summary>
    [TestCase("name: louis-agent-app")]
    [TestCase("image: acrpulsza.azurecr.io/louis-agent-site:TAG")]
    [TestCase("'APP_IDENTITY_ID': {}")]
    [TestCase("storageName: louis-agent-vol")]
    [TestCase("secretRef: agent-api-key-louis-agent")]
    [TestCase("secretRef: admin-api-key-louis-agent")]
    [TestCase("# SECRETS")]
    [TestCase("# PROVIDERS")]
    public void TheSecondAppsYaml_NamesWhatIsItsOwn(string line) =>
        Assert.That(CodingApp.Select(l => l.Trim()), Does.Contain(line));

    /// <summary>The framework's site's yaml (F164.3), each line trimmed; it fails plainly while there is none.</summary>
    private static List<string> FrameworkApp()
    {
        string path = Path.Combine(TestPaths.RepoRoot, "src/site/deploy/azure/louis-agent-framework-app.yaml");
        Assert.That(File.Exists(path), "src/site/deploy/azure/louis-agent-framework-app.yaml");
        return File.ReadAllLines(path).Select(l => l.Trim()).ToList();
    }

    /// <summary>
    /// The framework's yaml names its own app, image and keys, reads the vault with the shared identity, logs to the
    /// shared share in a folder of its own (Louis's pick A), opens the door to the framework Orbi at 20 a day (his
    /// pick A of three) and reads the docs its image carries; it keeps the markers the script fills.
    /// </summary>
    [TestCase("name: louis-agent-framework-app")]
    [TestCase("image: acrpulsza.azurecr.io/louis-agent-site-framework:TAG")]
    [TestCase("'APP_IDENTITY_ID': {}")]
    [TestCase("secretRef: anthropic-api-key")]
    [TestCase("secretRef: agent-api-key-framework")]
    [TestCase("secretRef: admin-api-key-framework")]
    [TestCase("value: louis-agent-framework")]
    [TestCase("value: '20'")]
    [TestCase("value: /app/docs")]
    [TestCase("storageName: louis-agent-vol")]
    [TestCase("subPath: framework/logs")]
    [TestCase("mountPath: /logs")]
    [TestCase("# SECRETS")]
    public void TheFrameworksYaml_NamesWhatIsItsOwn_AndWhatItShares(string line) =>
        Assert.That(FrameworkApp(), Does.Contain(line));

    /// <summary>
    /// The framework's site sells nothing and keeps nothing of its own: no lead mail, no CV, no peers to ask, no VM
    /// and no database, and only the logs on the share, never louis-agent.dev's own folders.
    /// </summary>
    [TestCase("LEADS_TO")]
    [TestCase("RESEND_API_KEY")]
    [TestCase("SITE_DATA_DIRECTORY")]
    [TestCase("AGENT_PEERS")]
    [TestCase("mountPath: /data")]
    [TestCase("subPath: logs")]
    [TestCase("VM_IP")]
    [TestCase("ACCOUNTS_SQL_URL")]
    public void TheFrameworksYaml_HoldsNothingItDoesntUse(string text) =>
        Assert.That(FrameworkApp(), Has.None.Contains(text));

    /// <summary>
    /// The site runs the guide alone (F124), so it mounts no workspace: the image carries the docs and the price
    /// tables, and the share keeps only the logs.
    /// </summary>
    [Test]
    public void TheSitesYaml_MountsTheLogsOnly()
    {
        var lines = CodingApp.Select(l => l.Trim()).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(lines, Does.Not.Contain("- name: WORKSPACE_ROOT").And.Not.Contain("mountPath: /workspace"));
            Assert.That(lines, Does.Contain("mountPath: /logs"));
        });
    }

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
