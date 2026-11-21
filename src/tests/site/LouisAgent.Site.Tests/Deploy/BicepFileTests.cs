namespace LouisAgent.Site.Tests.Deploy;

/// <summary>
/// Holds the framework's own Bicep file, which describes what the two sites stand on in Azure (F170): it names no
/// product, every name it takes has its value in our parameters file, and every module is pinned to a version.
/// </summary>
/// <remarks>Plain string checks on the files' lines, no regex (ADR 0057).</remarks>
public class BicepFileTests
{
    /// <summary>Where the file, its parameters and the deploy script sit, beside the two apps' yaml.</summary>
    private const string Folder = "src/site/deploy/azure";

    /// <summary>The start of a parameter's line, in the file and in the parameters file alike.</summary>
    private const string ParamWord = "param ";

    /// <summary>The start of a reference to a module in Bicep's public registry.</summary>
    private const string PublicModule = "'br/public:";

    /// <summary>The file's lines, or none while it doesn't exist yet, so each check fails on its own assertion.</summary>
    private static string[] Lines(string name)
    {
        string path = Path.Combine(TestPaths.RepoRoot, Folder, name);
        return File.Exists(path) ? File.ReadAllLines(path) : [];
    }

    /// <summary>The file.</summary>
    private static string[] Bicep => Lines("main.bicep");

    /// <summary>Our own instance's values.</summary>
    private static string[] Params => Lines("main.bicepparam");

    /// <summary>The name a parameter's line declares or sets: the word after <c>param</c>.</summary>
    private static string ParamName(string line) =>
        line[ParamWord.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

    /// <summary>The names the file takes, from its own lines (Louis's pick 2 of F170: every name a parameter).</summary>
    private static List<string> Declared() =>
        Bicep.Where(line => line.StartsWith(ParamWord, StringComparison.Ordinal)).Select(ParamName).ToList();

    /// <summary>
    /// Every module the file calls, as the text between its quotes, such as
    /// <c>br/public:avm/res/operational-insights/workspace:0.16.2</c>.
    /// </summary>
    private static List<string> Modules() =>
        Bicep.Where(line => line.StartsWith("module ", StringComparison.Ordinal) && line.Contains(PublicModule))
            .Select(line => line[(line.IndexOf(PublicModule, StringComparison.Ordinal) + 1)..])
            .Select(rest => rest[..rest.IndexOf('\'', StringComparison.Ordinal)])
            .ToList();

    /// <summary>
    /// A framework file names no product (ADR 0076): our own names, which still say Puls, live in the parameters
    /// file alone (Louis's pick 2 of F170).
    /// </summary>
    [Test]
    public void TheBicepNamesNoProduct()
    {
        Assert.That(Bicep, Is.Not.Empty, "main.bicep exists");
        Assert.That(Bicep.Where(line => line.Contains("puls", StringComparison.OrdinalIgnoreCase)), Is.Empty);
    }

    /// <summary>Every name the file takes has its value in our parameters file, so a deploy never asks for one.</summary>
    [Test]
    public void EveryParameter_HasItsValue()
    {
        var set = Params.Where(line => line.StartsWith(ParamWord, StringComparison.Ordinal)).Select(ParamName);

        Assert.That(Declared(), Is.Not.Empty, "the file takes its names as parameters");
        Assert.That(Declared().Except(set), Is.Empty);
    }

    /// <summary>
    /// Every module comes from Microsoft's verified modules at a version of its own (Louis's pick 1 of F170), so
    /// nothing changes under us between two deploys.
    /// </summary>
    [Test]
    public void EveryModule_IsPinned()
    {
        var modules = Modules();

        Assert.That(modules, Is.Not.Empty, "the file calls the verified modules");
        Assert.That(modules.Where(module => !module.StartsWith("br/public:avm/res/", StringComparison.Ordinal)
                || !Version.TryParse(module[(module.LastIndexOf(':') + 1)..], out _)),
            Is.Empty);
    }

    /// <summary>
    /// Application Insights keeps masking visitors' IP addresses, as it does today: the module's default would turn
    /// that off and start keeping them.
    /// </summary>
    [Test]
    public void TheInsights_KeepMaskingIpAddresses() =>
        Assert.That(Bicep.Select(line => line.Trim()), Does.Contain("disableIpMasking: false"));

    /// <summary>
    /// The vault's purge protection stays off, as it is today: the module's default would turn it on, and once on
    /// it can never be turned off.
    /// </summary>
    [Test]
    public void TheVault_HasNoPurgeProtection() =>
        Assert.That(Bicep.Select(line => line.Trim()), Does.Contain("enablePurgeProtection: false"));

    /// <summary>
    /// The registry denies no address: the module's default rule set would deny every one, and the environment's
    /// pulls of the apps' images with them.
    /// </summary>
    [Test]
    public void TheRegistry_LetsTheEnvironmentPull() =>
        Assert.That(Bicep.Select(line => line.Trim()), Does.Contain("networkRuleSetDefaultAction: 'Allow'"));

    /// <summary>
    /// The storage account keeps its key access: the environment mounts the apps' share with the account's key, so
    /// without it the sites could no longer write their logs.
    /// </summary>
    [Test]
    public void TheStorage_KeepsTheKeyTheShareIsMountedWith() =>
        Assert.That(Bicep.Select(line => line.Trim()), Does.Contain("allowSharedKeyAccess: true"));

    /// <summary>
    /// Every output the deploy script reads is one the file declares, so a release finds the environment, the
    /// identity and the vault it was deployed with.
    /// </summary>
    [Test]
    public void EveryOutputTheScriptReads_IsDeclared()
    {
        var read = ScriptReads("Get-Output '").Concat(ScriptReads("IdentityOutput = '")).Distinct().ToList();
        var declared = Bicep.Where(line => line.StartsWith("output ", StringComparison.Ordinal))
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);

        Assert.That(read, Is.Not.Empty, "the script reads outputs");
        Assert.That(read.Except(declared), Is.Empty);
    }

    /// <summary>The names the script quotes after a marker, such as <c>Get-Output 'vaultUri'</c>.</summary>
    private static IEnumerable<string> ScriptReads(string marker)
    {
        string script = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, Folder, "deploy.ps1"));
        for (int at = script.IndexOf(marker, StringComparison.Ordinal); at >= 0;
             at = script.IndexOf(marker, at + marker.Length, StringComparison.Ordinal))
        {
            int start = at + marker.Length;
            yield return script[start..script.IndexOf('\'', start)];
        }
    }
}
