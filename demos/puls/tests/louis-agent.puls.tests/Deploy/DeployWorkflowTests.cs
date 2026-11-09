namespace LouisAgent.Puls.Tests.Deploy;

/// <summary>
/// Holds the GitHub workflow that deploys Puls (F109) to Louis's three picks: it signs in to Azure by OpenID Connect
/// with no password stored, it runs <c>deploy.ps1 release</c> and no other verb, and it deploys by itself only after CI
/// passes on <c>main</c>, one deploy at a time, never cancelling one half-way.
/// </summary>
/// <remarks>
/// Plain string checks on the file's lines, no regex (ADR 0057), as <see cref="DeployFilesTests"/> does.
/// </remarks>
public class DeployWorkflowTests
{
    private static readonly string WorkflowPath =
        Path.Combine(TestPaths.RepoRoot, ".github/workflows/deploy-puls.yml");

    private static string[] Lines() => File.Exists(WorkflowPath) ? File.ReadAllLines(WorkflowPath) : [];

    private static IEnumerable<string> Trimmed() => Lines().Select(line => line.Trim());

    /// <summary>A short-lived token GitHub signs per run, and nothing that looks like a stored password.</summary>
    [Test]
    public void TheWorkflow_SignsInWithoutAPassword()
    {
        Assert.That(Trimmed(), Does.Contain("id-token: write"), "the OIDC token needs id-token: write");
        Assert.That(Trimmed(), Has.Some.Contains("uses: azure/login@"));
        Assert.That(Trimmed(), Has.None.Contains("client-secret").And.None.StartsWith("creds:"));
    }

    /// <summary>The one verb the deploy identity is for; infra, vm, secrets and domain stay on Louis's PC.</summary>
    [Test]
    public void TheWorkflow_RunsReleaseOnly()
    {
        var calls = Trimmed().Where(line => line.Contains("deploy.ps1", StringComparison.Ordinal)).ToList();

        Assert.That(calls, Is.Not.Empty);
        Assert.That(calls, Is.All.Contains("deploy.ps1 release"));
    }

    /// <summary>
    /// The merge deploys the louis-agent app alone (FX122, Louis: "remove everything puls related from azure and
    /// deployments"): one release, of that app, and the summary lists its revisions; Puls is not released.
    /// </summary>
    [Test]
    public void TheWorkflow_ReleasesTheSiteAlone()
    {
        var calls = Trimmed().Where(line => line.StartsWith("run:", StringComparison.Ordinal) &&
                                            line.Contains("deploy.ps1 release", StringComparison.Ordinal)).ToList();

        Assert.That(calls, Has.Count.EqualTo(1));
        Assert.That(calls[0], Does.Contain("-App louis-agent-app"));
        Assert.That(Trimmed(), Has.Some.Contains("-n louis-agent-app -g rg-puls-za"));
        Assert.That(Trimmed(), Has.None.Contains("puls-app"));
    }

    /// <summary>
    /// The deploy runs on the merge itself and from the button (FX93, Louis: "we only need it pull request build and
    /// test"): a push to main, never a pull request, and no wait on a CI run, since every pull request was tested with
    /// main merged in before it could merge.
    /// </summary>
    [Test]
    public void TheWorkflow_DeploysOnTheMerge()
    {
        var lines = Trimmed();

        Assert.That(lines, Does.Contain("push:").And.Contain("workflow_dispatch:"));
        Assert.That(lines, Does.Contain("branches: [ main ]"));
        Assert.That(lines, Has.None.StartsWith("workflow_run:").And.None.StartsWith("pull_request:"));
        Assert.That(lines, Has.None.Contains("workflow_run"), "nothing reads a CI run any more");
    }

    [Test]
    public void Deploys_QueueAndNeverCancel() =>
        Assert.That(Trimmed(), Does.Contain("cancel-in-progress: false"));
}
