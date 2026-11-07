namespace LouisAgent.Puls.Tests.Deploy;

/// <summary>
/// Holds the GitHub workflow that deploys Puls (F109) to Louis's three picks: it signs in to Azure by OpenID Connect
/// with no password stored, it runs <c>deploy.ps1 release</c> and no other verb, and it deploys by itself only after CI
/// passes on <c>main</c>, one deploy at a time, never cancelling one half-way.
/// </summary>
/// <remarks>Plain string checks on the file's lines, no regex (ADR 0057), as <see cref="DeployFilesTests"/> does.</remarks>
public class DeployWorkflowTests
{
    private static readonly string WorkflowPath = Path.Combine(TestPaths.RepoRoot, ".github/workflows/deploy-puls.yml");

    private static string[] Lines() => File.Exists(WorkflowPath) ? File.ReadAllLines(WorkflowPath) : [];

    private static IEnumerable<string> Trimmed() => Lines().Select(line => line.Trim());

    /// <summary>A short-lived token GitHub signs per run, and nothing that looks like a stored password.</summary>
    [Test]
    public void TheWorkflow_SignsInWithoutAPassword()
    {
        Assert.That(Trimmed(), Does.Contain("id-token: write"), "the OIDC token needs id-token: write");
        Assert.That(Trimmed(), Has.Some.StartsWith("uses: azure/login@"));
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

    /// <summary>Automatic only after green CI on main (Louis's pick), and a button for any time.</summary>
    [Test]
    public void TheWorkflow_DeploysOnlyAfterGreenCiOnMain()
    {
        var lines = Trimmed().ToList();

        Assert.That(lines, Does.Contain("workflow_run:").And.Contain("workflow_dispatch:"));
        Assert.That(lines, Does.Contain("workflows: [ CI ]").And.Contain("branches: [ main ]"));
        Assert.That(lines, Has.Some.Contains("github.event.workflow_run.conclusion == 'success'"));
        Assert.That(lines, Has.None.StartsWith("push:").And.None.StartsWith("pull_request:"));
    }

    /// <summary>A second deploy waits for the first; a deploy stopped half-way could leave a revision half-made.</summary>
    [Test]
    public void Deploys_QueueAndNeverCancel() =>
        Assert.That(Trimmed(), Does.Contain("cancel-in-progress: false"));
}
