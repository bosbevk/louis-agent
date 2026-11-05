using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Skills;
using LouisAgent.Identities.Coding;
using LouisAgent.Packs.DevOps;
using LouisAgent.Packs.Paymo;

namespace LouisAgent.Coding.Tests;

/// <summary>What this demo loads, registered before any test reads a prompt, as a host does when it starts.</summary>
[SetUpFixture]
public class CodingPrompts
{
    /// <summary>The demo's catalogue, with keys that are never used: nothing is called while building an engine.</summary>
    public static AgentCatalog Catalog { get; } = new([new CodingIdentity()],
        CodingCatalog.Packs(new PaymoOptions("snapshot"), new DevOpsOptions("snapshot", "org", "project")));

    /// <summary>Registers its prompts.</summary>
    [OneTimeSetUp]
    public void Register() => SkillFileSource.Use(Catalog.Prompts);
}
