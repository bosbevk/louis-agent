namespace LouisAgent.Site.Tests.Deploy;

/// <summary>
/// Holds what releases the framework's two sites: the deploy script beside their yaml, which knows both apps and
/// offers only the verbs that release them, and each app's yaml, which names what is its own and what it shares
/// (F164.3, F166.1). The old VM and Bicep build stay with Puls, and Puls's tests hold them.
/// </summary>
/// <remarks>Plain string checks on the files' lines, no regex (ADR 0057).</remarks>
public class DeployFilesTests
{
    /// <summary>The deploy script, one for both apps, beside their yaml (F166.1): every verb takes -App.</summary>
    private static readonly string Script = File.ReadAllText(
        Path.Combine(TestPaths.RepoRoot, "src/site/deploy/azure/deploy.ps1"));

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
    /// The script offers only what releases the two sites (F166.1, Louis's pick A of three): the old Bicep build's
    /// infra, vm and sql-access, which would make the VM, Mongo and SQL again, are gone with Puls.
    /// </summary>
    [Test]
    public void TheScript_OffersOnlyTheSitesVerbs() =>
        Assert.That(Script,
            Does.Contain("[ValidateSet('build', 'workspace', 'secrets', 'update', 'release', 'domain', 'logs')]")
                .And.Not.Contain("'infra' {").And.Not.Contain("'vm' {").And.Not.Contain("'sql-access' {"));

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
    /// The framework's entry names the folders its share must hold before its revision mounts one (F164.3, Louis's pick
    /// B of three): framework, then framework/logs, a parent before its child.
    /// </summary>
    [Test]
    public void TheFrameworksEntry_NamesTheFoldersItsLogsNeed() =>
        Assert.That(FrameworkEntry(), Does.Contain("Folders = @('framework', 'framework/logs')"));

    /// <summary>
    /// A release makes the folders its app names on the share before the revision, so a first release finds its logs
    /// folder; the storage key is read only when there are folders to make, so louis-agent.dev's release, which names
    /// none, never reads it.
    /// </summary>
    [TestCase("if ($AppInfo.Folders.Count -gt 0) {")]
    [TestCase("foreach ($folder in $AppInfo.Folders) {")]
    [TestCase(
        "Invoke-Az storage directory create --account-name 'pulsstorage' --account-key $key --share-name $AppInfo.Share -n $folder -o none")]
    public void TheRelease_MakesTheFoldersItsAppNames(string text)
    {
        int release = Script.IndexOf("    'release' {", StringComparison.Ordinal);
        int build = Script.IndexOf("& $PSCommandPath build", release, StringComparison.Ordinal);

        Assert.That(Script[release..build], Does.Contain(text));
    }

    /// <summary>louis-agent.dev's entry names no folders, so its release is as it was before F164.3.</summary>
    [Test]
    public void TheSitesEntry_NamesNoFolders()
    {
        int start = Script.IndexOf("'louis-agent-app' = @{", StringComparison.Ordinal);
        int end = Script.IndexOf("\n    }", start, StringComparison.Ordinal);

        Assert.That(Script[start..end], Does.Contain("Folders = @()"));
    }

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

    /// <summary>
    /// The framework's site's yaml (F164.3), each line trimmed; it fails plainly while there is none.
    /// </summary>
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
}
