using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Skills;
using LouisAgent.Identities.Coding;
using LouisAgent.Identities.Expert;
using LouisAgent.Identities.Triage;
using LouisAgent.Packs.Docs;
using LouisAgent.Packs.Leads;
using LouisAgent.Packs.DevOps;
using LouisAgent.Packs.Paymo;
using LouisAgent.Packs.Triage;

namespace LouisAgent.Coding.Tests;

/// <summary>What this demo loads, registered before any test reads a prompt, as a host does when it starts.</summary>
[SetUpFixture]
public class CodingPrompts
{
    /// <summary>The demo's catalogue, with keys that are never used: nothing is called while building an engine.</summary>
    public static AgentCatalog Catalog { get; } = new(
        [new CodingIdentity(), new ExpertIdentity(), new TriageIdentity()],
        CodingCatalog.Packs(new PaymoOptions("snapshot"), new DevOpsOptions("snapshot", "org", "project"),
            new DocsOptions { Root = "snapshot-docs" },
            // The departments file is read when the tools are built, so it is the demo's real one.
            new TriageOptions
            {
                Runs = "snapshot-runs",
                People = "snapshot-people",
                Departments = Path.Combine(TestPaths.RepoRoot, "demos/louis-agent/config/departments.yaml")
            },
            new LeadsOptions { To = "snapshot@example.test" }));

    /// <summary>Registers its prompts.</summary>
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use(Catalog.Prompts);
}
